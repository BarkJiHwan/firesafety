using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [SerializeField] TutorialDataMgr dataMgr;

    [Header("원 회전")]
    // 플레이어를 중심으로 화살표가 회전하는 반지름
    [SerializeField] float radius = 2f;
    // 회전 속도
    [SerializeField] float speed = 5f;
    [SerializeField] float appearDuration = 1f;
    // 화살표의 높이 오프셋
    [SerializeField] float heightOffset = 0.7f;
    // 화살표 회전에 추가할 로컬 오일러 회전값
    [SerializeField] Vector3 rotArrow;

    [Header("날아가기")]
    // 목표 지점까지 날아가는 시간
    [SerializeField] float flyDuration = 1.0f;

    // 플레이어 위치
    Transform playerPos;
    // TutorialDataMgr의 GetInteractObject를 받아야 하는데...
    Transform targetPos;
    float timeElasped;

    PlayerTutorial[] turtorialMgr;
    PlayerTutorial myTutorialMgr;

    CreateArrow createArrow;
    bool isAlreadyMade = false;

    private void Awake()
    {
        createArrow = GetComponent<CreateArrow>();
    }

    void Start()
    {
        //if(createArrow != null)
        //{
        //    createArrow.MakeArrow();
        //}
    }

    void Update()
    {
        // 로컬 플레이어 튜토리얼 매니저 탐색 및 이벤트 바인딩
        if (turtorialMgr == null || turtorialMgr.Length == 0)
        {
            turtorialMgr = FindObjectsOfType<PlayerTutorial>();
        }
        if (turtorialMgr != null && isAlreadyMade == false)
        {
            foreach(var tutMgr in turtorialMgr)
            {
                PhotonView view = tutMgr.gameObject.GetComponent<PhotonView>();
                // 나의 튜토리얼이면
                if(view != null && view.IsMine)
                {
                    myTutorialMgr = tutMgr;
                    tutMgr.arrowCtrl = this;
                    // 튜토리얼 안의 화살표 출발하는 이벤트 구독
                    tutMgr.OnStartArrow += AppearArrow;
                    isAlreadyMade = true;
                }
            }
        }
    }

    // 타겟 향해 회전 + 날아가는 애니메이션 처리 코루틴
    IEnumerator GuideArrowToTarget(Transform targetPos)
    {
        // 시간 초기화
        timeElasped = 0;
        // 플레이어 위치 가져오기
        playerPos = myTutorialMgr.transform;
        // 플레이어 정면 방향 벡터
        Vector3 forward = playerPos.forward;
        // 목표 위치 방향 벡터 (플레이어 -> 목표 지점)
        Vector3 dirToTarget = (targetPos.position - playerPos.position).normalized;

        // 시작 방향과 목표 방향 사이의 회전 쿼터니언 계산
        Quaternion startRot = Quaternion.LookRotation(forward);
        Quaternion targetRot = Quaternion.LookRotation(dirToTarget);

        // 두 방향 사이의 각도
        float angle = Vector3.Angle(forward, dirToTarget);
        float currentAngle = 0;
        // 화살표가 원 궤도를 따라 회전하는 부분
        while (currentAngle < angle)
        {
            // 회전 속도만큼 현재 각도 증가
            float stepAngle = speed * Time.deltaTime;
            currentAngle += stepAngle;

            // 현재 회전 진행율 비율 (0 ~ 1)
            float t = Mathf.Clamp01(currentAngle / angle);

            // 현재 방향을 Slerp로 계산 -> 원호 경로 따라가게 됨
            Vector3 currentDir = Vector3.Slerp(forward, dirToTarget, t);
            // 원의 위치 계산 (플레이어를 기준으로 반지름 거리만큼 떨어진 곳)
            Vector3 circlePos = playerPos.position + currentDir * radius;
            // 화살표 위치 설정 (y축은 고정 높이 사용)
            transform.position = new Vector3(circlePos.x, heightOffset, circlePos.z);

            // 방향 회전 보간
            Quaternion rot = Quaternion.Slerp(startRot, targetRot, t);

            // rotArrow 오프셋 적용 (화살표가 정면을 기준으로 특정 각도만큼 틀어져야 할 경우)
            Quaternion fixedRot = rot * Quaternion.Euler(rotArrow.x, rotArrow.y, rotArrow.z);
            transform.rotation = fixedRot;

            yield return null;
        }

        // 목표 지점을 향해 날아감 (직선 이동)

        // 현재 위치를 시작점으로 
        Vector3 startPos = transform.position;

        // 목표 위치를 높이 포함한 최종 지점으로 설정
        Vector3 goalPos = targetPos.position + Vector3.up * heightOffset;

        timeElasped = 0;
        while(timeElasped < flyDuration)
        {
            // 시간 누적
            timeElasped += Time.deltaTime;
            // 보간 비율 계산
            float t = Mathf.Clamp01(timeElasped / flyDuration);

            // 직선 위치 이동
            transform.position = Vector3.Lerp(startPos, goalPos, t);

            // 현재 방향을 기준으로 회전
            Vector3 dir = (goalPos - transform.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = rot * Quaternion.Euler(rotArrow.x, rotArrow.y, rotArrow.z);

            yield return null;
        }

        // 최종 위치에서 회전 고정 (화살표가 똑바로 누운 상태로)
        transform.rotation = Quaternion.Euler(0, 0, -90);
    }

    // 외부에서 호출되어 화살표 생성 및 이동을 시작하는 메서드
    void AppearArrow(int playerIndex)
    {
        Debug.Log("AppearArrow 들어옴");
        // 둘 다 TutorialMgr에 있음
        // playerPos : Player 받아와야 함
        playerPos = myTutorialMgr.transform;
        // targetPos : 타겟 받아와야 함
        targetPos = dataMgr.GetInteractObject(playerIndex).transform;

        // 화살표 생성
        if (createArrow != null)
        {
            createArrow.MakeArrow();
        }

        //Quaternion rot = Quaternion.LookRotation(playerPos.forward);
        //Quaternion fixedRot = rot * Quaternion.Euler(rotArrow.x, rotArrow.y, rotArrow.z);
        //transform.rotation = fixedRot;

        // 회전 및 날아가기 코루틴 시작
        StartCoroutine(GuideArrowToTarget(targetPos));
    }

    // 비활성화시
    private void OnDisable()
    {
        if(myTutorialMgr == null)
        {
            return;
        }
        // 돌고 있는 모든 코루틴 중지
        StopAllCoroutines();
        // 화살표 생성해서 날아가는 이벤트 구독 해지
        myTutorialMgr.OnStartArrow -= AppearArrow;
    }
}
