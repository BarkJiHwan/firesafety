using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] Image ConverBackImage;
    [SerializeField] Color converBackColor;

    [Header("대화창 대화 Text")]
    [SerializeField] TextMeshProUGUI preventWord;
    [SerializeField] float fontSize;
    [SerializeField] Color fontColor;

    [Header("아이콘")]
    [SerializeField] Image iconImg;
    [SerializeField] Sprite warningIcon;
    [SerializeField] Sprite completeIcon;

    RectTransform rect;
    GameManager gameManager;
    Vector3 basicPos;
    FirePreventable currentPrevent;
    bool isPointing;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void Start()
    {
        // 초기 세팅
        backImage.color = backColor;
        ConverBackImage.color = converBackColor;
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
        if(GameManager.Instance.CurrentPhase != GamePhase.Prevention)
        {
            if(backImage.gameObject.activeSelf == true)
            {
                backImage.gameObject.SetActive(false);
                iconImg.gameObject.SetActive(false);
            }
        }

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
        var target = args.interactableObject;
        //FirePreventable prevent = target.transform.GetComponent<FirePreventable>();
        currentPrevent = target.transform.GetComponent<FirePreventable>();
        FireObjScript fire = target.transform.GetComponent<FireObjScript>();

        //transform.position = target.transform.position + fire.SpawnOffset;
        Vector3 originPosition = fire.TaewooriPos();
        transform.position = originPosition + basicPos;

        Vector3 targetForward = target.transform.forward;
        if(targetForward != Vector3.zero)
        {
            targetForward.y = 0;
            targetForward.Normalize();
        }

        Vector3 camDir = Camera.main.transform.position - target.transform.position;
        if(camDir != Vector3.zero)
        {
            camDir.Normalize();
        }

        float dot = Vector3.Dot(targetForward, camDir);
        if (dot > 0)
        {
            transform.forward = -targetForward;
        }
        else if (targetForward != Vector3.zero)
        {
            transform.forward = targetForward;
        }

        Vector3 canvasForward = transform.forward;
        Vector3 toCam = (Camera.main.transform.position - transform.position).normalized;

        float dots = Vector3.Dot(canvasForward, toCam);
        if (dots > 0)
        {
            transform.Rotate(0, 180, 0);
        }

        if (currentPrevent.MyType == PreventType.ElectricKettle || currentPrevent.MyType == PreventType.PowerBank)
        {
            transform.position = originPosition + basicPos;
            transform.Rotate(0, 0, 0);
        }

        else if(currentPrevent.MyType == PreventType.OldWire)
        {
            transform.position = originPosition + new Vector3(0, 0, 1);
        }

        else if (IsUIBlocked())
        {
            MoveUIPosition(originPosition);
        }

        // ray로 계속 쏘고 있으면
        isPointing = true;
        RefreshUI();
    }

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
        List<Vector3> rayPoints = new List<Vector3>
        {
            rect.position,
            worldCorners[0],
            worldCorners[1],
            worldCorners[2],
            worldCorners[3],
        };
        int blockedCount = 0;

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

    void MoveUIPosition(Vector3 originPos)
    {
        List<Vector3> candidatePos = new List<Vector3>
        {
            originPos + new Vector3(uiPos * 2, 0, 0),
            originPos + new Vector3(0, -uiPos / 2, 0),
            originPos + new Vector3(-uiPos * 2, 0, 0),
            //originPos + new Vector3(0, 0, 1),
            //originPos + new Vector3(0, 0, -1)
        };

        foreach (var pos in candidatePos)
        {
            transform.position = pos;
            if(!IsUIBlocked())
            {
                return;
            }
        }
        transform.position = originPos + basicPos;
    }

    void RefreshUI()
    {
        if (currentPrevent == null)
        {
            return;
        }
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
}
