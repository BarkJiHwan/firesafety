using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class ObjectUICtrl : MonoBehaviour
{
    [SerializeField] float uiPos = 0.5f;
    [Header("제일 큰 배경")]
    [SerializeField] Image backImage;
    [SerializeField] Color backColor;

    [Header("대화창 배경")]
    [SerializeField] Image converBackImage;
    [SerializeField] Color converBackColor;

    [Header("대화창 대화 Text")]
    [SerializeField] TextMeshProUGUI preventWord;
    [SerializeField] float fontSize;
    [SerializeField] Color fontColor;

    [Header("아이콘")]
    [SerializeField] Image iconImg;
    [SerializeField] Sprite warningIcon;
    [SerializeField] Sprite completeIcon;
    [SerializeField] Sprite triggerButtonIcon;

    RectTransform rect;
    GameManager gameManager;
    Vector3 basicPos;
    FirePreventable currentPrevent;
    bool isPointing;
    PlayerTutorial[] turtorialMgr;
    PlayerTutorial myTutorialMgr;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void Start()
    {
        // 초기 세팅
        backImage.color = backColor;
        converBackImage.color = converBackColor;
        preventWord.fontSize = fontSize;
        preventWord.color = fontColor;
        iconImg.sprite = warningIcon;

        // 배경 비활성화
        backImage.gameObject.SetActive(false);
        // 아이콘 비활성화
        iconImg.gameObject.SetActive(false);
        // 오브젝트 위에 떠야 하는 UI 기본 위치
        basicPos = new Vector3(0, uiPos, 0);
    }

    void Update()
    {
        // 튜토리얼 매니저가 없으면 씬에서 찾아 초기화
        if (turtorialMgr == null || turtorialMgr.Length == 0)
        {
            turtorialMgr = FindObjectsOfType<PlayerTutorial>();
        }

        // 로컬 플레이어의 튜토리얼 매니저 연결 및 이벤트 등록
        if (turtorialMgr != null && myTutorialMgr == null)
        {
            foreach (var tutMgr in turtorialMgr)
            {
                PhotonView view = tutMgr.gameObject.GetComponent<PhotonView>();
                if (view != null && view.IsMine)
                {
                    myTutorialMgr = tutMgr;
                    // 튜토리얼 매니저의 이벤트 구독
                    // 튜토리얼 예방 오브젝트 관련 이벤트
                    myTutorialMgr.OnObjectUI += TutorialPreventObject;
                    // 튜토리얼 예방 오브젝트 위 아이콘 바꾸는 이벤트
                    myTutorialMgr.OnCompleteSign += ChangeTutorialIcon;
                    // 튜토리얼 끝나는 이벤트
                    myTutorialMgr.OnFinishTutorial += FinishTutorial;
                }
            }
        }

        // 화재 대기 단계가 되면 UI 비활성화
        if (GameManager.Instance.CurrentPhase == GamePhase.FireWaiting)
        {
            if(gameObject.activeSelf == true)
            {
                gameObject.SetActive(false);
            }
        }

        // 포인팅 중이고, 예방 가능한 오브젝트일 경우 UI 갱신
        if(currentPrevent != null)
        {
            if(isPointing == true && currentPrevent.IsFirePreventable == true)
            {
                RefreshUI();
            }
        }
    }

    public void SelectedObject(HoverEnterEventArgs args)
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Prevention)
        {
            return;
        }
        var targetObject = args.interactableObject;
        Transform target = targetObject.transform;
        //FirePreventable prevent = target.transform.GetComponent<FirePreventable>();

        PositionUI(target); // UI 위치 조정

        // ray로 계속 쏘고 있으면
        isPointing = true;
        RefreshUI(); // UI 내용 업데이트
    }

    public void PositionUI(Transform targetPos)
    {
        currentPrevent = targetPos.GetComponent<FirePreventable>();
        Vector3 originPosition = Vector3.zero;
        if (currentPrevent == null)
        {
            // 일반 오브젝트일 경우 위치만 설정
            originPosition = targetPos.position + new Vector3(0, 0.3f, 0);
            transform.position = originPosition + basicPos;

            // 카메라를 바라보도록 회전
            Vector3 cam = Camera.main.transform.position - transform.position;
            cam.y = 0;
            transform.rotation = Quaternion.LookRotation(cam) * Quaternion.Euler(0, 180, 0);
            return;
        }
        else
        {
            // 예방 대상 오브젝트 위치 계산
            FireObjScript fire = targetPos.GetComponent<FireObjScript>();

            //transform.position = target.transform.position + fire.SpawnOffset;
            originPosition = fire.TaewooriPos();
        }
        transform.position = originPosition + basicPos;

        // UI 방향 계산 (오브젝트 기준)
        Vector3 targetForward = targetPos.forward;
        if (targetForward != Vector3.zero)
        {
            targetForward.y = 0;
            targetForward.Normalize();
        }

        // 카메라 방향 계산
        Vector3 camDir = Camera.main.transform.position - targetPos.position;
        if (camDir != Vector3.zero)
        {
            camDir.Normalize();
        }

        // 카메라와 오브젝트의 상대 방향에 따라 UI 방향 결정
        float dot = Vector3.Dot(targetForward, camDir);
        if (dot > 0)
        {
            transform.forward = -targetForward;
        }
        else if (targetForward != Vector3.zero)
        {
            transform.forward = targetForward;
        }

        // UI가 카메라를 등지지 않도록 조정
        Vector3 canvasForward = transform.forward;
        Vector3 toCam = (Camera.main.transform.position - transform.position).normalized;

        float dots = Vector3.Dot(canvasForward, toCam);
        if (dots > 0)
        {
            transform.Rotate(0, 180, 0);
        }

        // 주전자일 경우 위치 조정
        if (currentPrevent.MyType == PreventType.ElectricKettle)
        {
            transform.position = originPosition + basicPos;
            transform.Rotate(0, 0, 0);
        }
        // 오래된 선일 경우 위치 조정
        else if (currentPrevent.MyType == PreventType.OldWire)
        {
            transform.position = originPosition + new Vector3(0, 0, 1);
        }

        // 장애물(대채로 벽)로 UI가 가려지면 위치 이동
        else if (IsUIBlocked())
        {
            MoveUIPosition(originPosition);
        }
    }

    // 오브젝트 선택 해제 처리
    public void DisSelectedObject()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Prevention)
        {
            return;
        }
        isPointing = false;
        backImage.gameObject.SetActive(false);
        iconImg.gameObject.SetActive(false);
    }

    // UI가 다른 오브젝트 뒤에 가려져 있으면 플레이어가 못 보기 때문에
    // 카메라 위치에서 UI 위치까지 Ray를 쏴서 중간에 장애물이 있는지 체크
    bool IsUIBlocked()
    {
        Vector3[] worldCorners = new Vector3[4];
        rect = GetComponent<RectTransform>();
        rect.GetWorldCorners(worldCorners);

        // 중심 + 4코너에서 ray 쏨
        List<Vector3> rayPoints = new List<Vector3>
        {
            rect.position,
            worldCorners[0],
            worldCorners[1],
            worldCorners[2],
            worldCorners[3],
        };
        int blockedCount = 0;

        // 각각의 Ray에 뭐가 닿으면 blockedCount 증가
        foreach(var point in rayPoints)
        {
            float distance = 0.7f;

            if (Physics.Raycast(point, transform.forward, out RaycastHit hit, distance))
            {
                if(!hit.transform.IsChildOf(rect))
                {
                    blockedCount++;
                }
            }
        }

        return blockedCount > 0;
    }

    // UI 위치 보정
    void MoveUIPosition(Vector3 originPos)
    {
        // UI 후보지 List에 등록
        List<Vector3> candidatePos = new List<Vector3>
        {
            originPos + new Vector3(uiPos * 2, 0, 0),
            originPos + new Vector3(0, -uiPos / 2, 0),
            originPos + new Vector3(-uiPos * 2, 0, 0),
            //originPos + new Vector3(0, 0, 1),
            //originPos + new Vector3(0, 0, -1)
        };

        // 각각의 후보 위치에서 UI가 가려지면 Return
        foreach (var pos in candidatePos)
        {
            transform.position = pos;
            if(!IsUIBlocked())
            {
                return;
            }
        }

        // 어디에도 위치 못 잡으면 기본 위치로 복귀
        transform.position = originPos + basicPos;
    }

    void RefreshUI()
    {
        if (currentPrevent == null)
        {
            return;
        }
        // 텍스트 업데이트
        preventWord.text = currentPrevent.ShowText();
        // 예방 됐을 때 아이콘 변경
        if (currentPrevent.IsFirePreventable == true)
        {
            iconImg.sprite = completeIcon;
        }
        else
        {
            iconImg.sprite = warningIcon;
        }

        // 배경 활성화 (예방이 됐으면 안 나와야 함)
        backImage.gameObject.SetActive(!currentPrevent.IsFirePreventable);
        // 아이콘 활성화
        iconImg.gameObject.SetActive(true);
    }

    void TutorialPreventObject(GameObject preventObject)
    {
        // 아이콘 이미지 Scale 변경
        iconImg.rectTransform.localScale = new Vector3(2f, 2f, 1);
        // 활성화
        iconImg.sprite = triggerButtonIcon;
        // 활성화
        iconImg.gameObject.SetActive(true);

        PositionUI(preventObject.transform);
    }

    // 튜토리얼에 예방 오브젝트 위에 뜨는 UI 아이콘 변경
    void ChangeTutorialIcon()
    {
        iconImg.rectTransform.localScale = Vector3.one;
        iconImg.sprite = completeIcon;
    }

    // 튜토리얼 끝났을 때
    void FinishTutorial()
    {
        // 변경된 아이콘 이미지 Scale 원상복귀
        iconImg.rectTransform.localScale = Vector3.one;
        iconImg.gameObject.SetActive(false);
        // 튜토리얼 구독했던 거 해제
        myTutorialMgr.OnObjectUI -= TutorialPreventObject;
        myTutorialMgr.OnCompleteSign -= ChangeTutorialIcon;
    }

    private void OnDisable()
    {
        // 비활성화되면 튜토리얼 구독 해제
        myTutorialMgr.OnFinishTutorial -= FinishTutorial;
    }
}
