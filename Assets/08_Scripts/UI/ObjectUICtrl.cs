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
        basicPos = new Vector3(0, 0.5f, 0);
    }

    void Update()
    {
        
    }

    public void SelectedObject(HoverEnterEventArgs args)
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Prevention)
        {
            return;
        }
        var target = args.interactableObject;
        FirePreventable prevent = target.transform.GetComponent<FirePreventable>();
        FireObjScript fire = target.transform.GetComponent<FireObjScript>();

        //transform.position = target.transform.position + fire.SpawnOffset;
        Vector3 originPosition = fire.TaewooriPos();
        transform.position = originPosition + basicPos;

        if(IsUIBlocked(Camera.main))
        {
            MoveUIPosition(originPosition);
        }

        // 예방이 안 됐을 때 문구 바꾸기
        preventWord.text = prevent.ShowText();
        // 예방 됐을 때 아이콘 변경
        if(prevent.IsFirePreventable == true)
        {
            iconImg.sprite = completeIcon;
        }
        else
        {
            iconImg.sprite = warningIcon;
        }

        // 배경 활성화 (예방이 됐으면 안 나와야 함)
        backImage.gameObject.SetActive(!prevent.IsFirePreventable);
        // 아이콘 활성화
        iconImg.gameObject.SetActive(true);
    }

    public void DisSelectedObject()
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Prevention)
        {
            return;
        }
        backImage.gameObject.SetActive(false);
        iconImg.gameObject.SetActive(false);
    }

    // UI가 다른 오브젝트 뒤에 가려져 있으면 플레이어가 못 보기 때문에
    // 카메라 위치에서 UI 위치까지 Ray를 쏴서 중간에 장애물이 있는지 체크
    bool IsUIBlocked(Camera cam)
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
            Vector3 dir = point - cam.transform.position;
            float distance = dir.magnitude;

            if(Physics.Raycast(cam.transform.position, dir.normalized, out RaycastHit hit, distance))
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
            originPos + new Vector3(1, 0, 0),
            originPos + new Vector3(0, -1, 0),
            originPos + new Vector3(1 * 2, 0, 0)
        };

        Debug.Log("부딪혔음");

        foreach (var pos in candidatePos)
        {
            transform.position = pos;
            Debug.Log("후보 위치 : " + pos);
            if(!IsUIBlocked(Camera.main))
            {
                Debug.Log("최종 위치 : " + pos);
                return;
            }
        }
        //transform.position = originPos + basicPos;
    }
}
