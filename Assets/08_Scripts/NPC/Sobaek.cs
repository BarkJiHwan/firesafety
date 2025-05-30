using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sobaek : MonoBehaviour
{
    #region 싱글톤
    public static Sobaek Instance { get; private set; }
    #endregion

    #region 인스펙터 설정
    [Header("기본 위치 설정")]
    [SerializeField] private Transform player; // VR 카메라 또는 플레이어 Transform
    [SerializeField] private float offsetDistance = 1.5f; // 플레이어와의 좌우 거리 (X축)
    [SerializeField] private float offsetHeight = 0.5f; // 플레이어 어깨 높이 (Y축)
    [SerializeField] private float offsetForward = 0.5f; // 플레이어 앞뒤 거리 (Z축, + = 앞쪽, - = 뒤쪽)
    [SerializeField] private bool stayOnRightSide = true; // 오른쪽에 고정

    [Header("둥둥 떠다니기 효과")]
    [SerializeField] private float floatAmplitude = 0.3f; // 위아래 움직임 크기
    [SerializeField] private float floatSpeed = 1f; // 위아래 움직임 속도
    [SerializeField] private float lookAtSpeed = 2f; // 플레이어 바라보는 회전 속도

    [Header("상호작용 이동 설정")]
    [SerializeField] private bool enableInteractionMovement = true; // 상호작용 이동 활성화
    [SerializeField] private float interactionMoveSpeed = 4f; // 상호작용 이동 속도
    [SerializeField] private float interactionOffset = 0.5f; // 상호작용 오브젝트에서 떨어진 거리
    [SerializeField] private float returnSpeed = 3f; // 돌아오는 속도
    #endregion

    #region 변수 선언
    private Vector3 homePosition; // 기본 대기 위치
    private Vector3 basePosition; // 둥둥 효과 기준 위치
    private Vector3 interactionTarget; // 상호작용 타겟 위치
    private bool isInteracting = false; // 상호작용 중인지
    private float floatTimer = 0f; // 둥둥 효과용 타이머
    #endregion

    #region 프로퍼티
    public Transform Player { get => player; set => player = value; }
    public bool IsInteracting => isInteracting;
    #endregion

    #region 유니티 라이프사이클
    void Start()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {           
            Destroy(gameObject);
            return;
        }

        InitializeReferences();
        SetHomePosition();
        basePosition = homePosition;

    }

    void Update()
    {
        if (player == null)
            return;

        UpdatePosition();
        UpdateFloatingEffect();
    }

    void OnDestroy()
    {
        // 싱글톤 해제
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// VR 레퍼런스들 초기화
    /// </summary>
    void InitializeReferences()
    {
        // 플레이어가 설정되지 않았다면 VR 카메라를 찾기
        if (player == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                player = mainCam.transform;
            }
            else
            {
                // Oculus/Meta Quest의 경우
                GameObject centerEye = GameObject.Find("CenterEyeAnchor");
                if (centerEye != null)
                {
                    player = centerEye.transform;
                }
            }
        }
    }
    #endregion

    #region 위치 및 이동 시스템
    /// <summary>
    /// 홈 포지션 설정 (플레이어 옆 고정 위치)
    /// </summary>
    void SetHomePosition()
    {
        if (player == null)
            return;

        // 좌우 방향 (X축)
        Vector3 rightDirection = player.right * (stayOnRightSide ? offsetDistance : -offsetDistance);

        // 앞뒤 방향 (Z축)
        Vector3 forwardDirection = player.forward * offsetForward;

        // 최종 홈 포지션 = 플레이어 위치 + 좌우오프셋 + 앞뒤오프셋 + 높이오프셋
        homePosition = player.position + rightDirection + forwardDirection + Vector3.up * offsetHeight;
    }

    /// <summary>
    /// 위치 업데이트 (홈 포지션 추적 또는 상호작용 이동)
    /// </summary>
    void UpdatePosition()
    {
        if (isInteracting)
        {
            // 상호작용 타겟으로 이동
            basePosition = Vector3.Lerp(basePosition, interactionTarget, interactionMoveSpeed * Time.deltaTime);
        }
        else
        {
            // 홈 포지션 업데이트 (플레이어가 움직이면 따라감)
            SetHomePosition();

            // 부드럽게 홈 포지션으로 이동
            basePosition = Vector3.Lerp(basePosition, homePosition, returnSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 소백이를 원래 위치로 즉시 이동
    /// </summary>
    public void TeleportToPlayer()
    {
        if (player != null)
        {
            isInteracting = false;
            SetHomePosition();
            basePosition = homePosition;           
        }
    }

    /// <summary>
    /// 소백이 위치를 왼쪽/오른쪽으로 전환
    /// </summary>
    public void ToggleSide()
    {
        stayOnRightSide = !stayOnRightSide;
        SetHomePosition();
    }
    #endregion

    #region 둥둥 효과 및 회전
    /// <summary>
    /// 둥둥 떠다니는 효과
    /// </summary>
    void UpdateFloatingEffect()
    {
        floatTimer += Time.deltaTime * floatSpeed;

        // 위아래 움직임
        float floatY = Mathf.Sin(floatTimer) * floatAmplitude;
        Vector3 floatingPosition = basePosition + Vector3.up * floatY;

        // 위치 적용
        transform.position = floatingPosition;

        // 플레이어를 바라보는 회전
        LookAtPlayerSmooth();
    }

    /// <summary>
    /// 부드럽게 플레이어를 바라보기
    /// </summary>
    void LookAtPlayerSmooth()
    {
        if (player == null)
            return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        // Y축 회전만 적용 (상하로 고개 너무 많이 돌리지 않게)
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                lookAtSpeed * Time.deltaTime
            );
        }
    }
    #endregion

    #region 상호작용 이동
    /// <summary>
    /// XRSimpleInteractable 컴포넌트 달린 오브젝트의 설정 오프셋으로 이동
    /// </summary>
    public void MoveToInteractionTarget(Transform target)
    {
        if (!enableInteractionMovement || target == null)
        {            
            return;
        }

        // 타겟에서 약간 떨어진 위치 계산
        Vector3 directionFromTarget = (transform.position - target.position).normalized;
        if (directionFromTarget == Vector3.zero)
        {
            directionFromTarget = Vector3.up;
        }

        interactionTarget = target.position + directionFromTarget * interactionOffset;
        isInteracting = true;
                
    }
 

    /// <summary>
    /// 소백이 원래 위치로 돌아가는 함수
    /// </summary>
    public void StopInteraction()
    {
        if (!enableInteractionMovement)
            return;

        isInteracting = false;
        
    }

    /// <summary>
    /// 상호작용 이동 활성화/비활성화
    /// </summary>
    public void SetInteractionMovement(bool enable)
    {
        enableInteractionMovement = enable;
        if (!enable)
        {
            StopInteraction();
        }
        
    }
    #endregion

    #region 설정 변경 메서드
    public void SetFloatingEffect(float amplitude, float speed)
    {
        floatAmplitude = amplitude;
        floatSpeed = speed;
    }

    public void SetLookAtSpeed(float speed)
    {
        lookAtSpeed = Mathf.Clamp(speed, 0.1f, 10f);
    }

    public void SetInteractionMoveSpeed(float speed)
    {
        interactionMoveSpeed = Mathf.Clamp(speed, 0.5f, 10f);
    }

    public void SetInteractionOffset(float offset)
    {
        interactionOffset = Mathf.Clamp(offset, 0.1f, 3f);
    }

    public void SetReturnSpeed(float speed)
    {
        returnSpeed = Mathf.Clamp(speed, 0.5f, 10f);
    }

    public void SetAllOffsets(float distance, float forward, float height)
    {
        offsetDistance = Mathf.Clamp(distance, 0.5f, 5f);
        offsetForward = Mathf.Clamp(forward, -3f, 3f);
        offsetHeight = Mathf.Clamp(height, -1f, 3f);
    }
    #endregion
   
}
