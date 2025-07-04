using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class CurvedButtonLimit : MonoBehaviour
{
    // 곡면 UI의 위치
    [SerializeField] Transform curvedMeshTrans;
    // 왼손, 오른손 XR Ray Interactor 배열
    [SerializeField] XRRayInteractor[] xrRay;

    GameObject curvedMesh;
    // 상호작용할 버튼
    Button screenButton;
    // 각 Ray의 부모 컨트롤러
    ActionBasedController[] controller;

    private void Awake()
    {
        // 곡면 오브젝트 캐싱
        curvedMesh = curvedMeshTrans.gameObject;
        screenButton = GetComponent<Button>();
    }

    private void Start()
    {
        controller = new ActionBasedController[xrRay.Length];
        // 각 Ray Interactor의 부모에서 ActionBasedCotroller 컴포넌트 가져오기
        controller[0] = xrRay[0].transform.parent.GetComponent<ActionBasedController>();
        controller[1] = xrRay[1].transform.parent.GetComponent<ActionBasedController>();
    }

    private void Update()
    {
        // null 체크
        if (curvedMeshTrans == null || xrRay == null)
        {
            return;
        }
        // 각 레이가 곡면 UI를 향하고 있는지 검사
        bool isRay1In = IsRayBelowCurvedMesh(xrRay[0].transform);
        bool isRay2In = IsRayBelowCurvedMesh(xrRay[1].transform);

        // 각 컨트롤러의 selectionAction (트리거)가 눌렸는지 확인 (0.5 이상이면 true)
        bool isTrigger1 = controller[0].selectAction.action.ReadValue<float>() > 0.5f;
        bool isTrigger2 = controller[1].selectAction.action.ReadValue<float>() > 0.5f;

        // 두 조건(레이 방향 + 트리거 입력)이 모두 만족되면 버튼 활성화
        if(isTrigger1 && isRay1In)
        {
            screenButton.interactable = true;
        }
        else if(isTrigger2 && isRay2In)
        {
            screenButton.interactable = true;
        }
        else
        {
            screenButton.interactable = false;
        }

    }

    // Ray 방향이 아래로 향하면 false 반환 (곡면 아래에서 쏘는 경우 무시)
    bool IsRayBelowCurvedMesh(Transform rayTransform)
    {
        // Ray 방향과 월드 아래 방향의 내적 결과
        float verticalDir = Vector3.Dot(rayTransform.forward.normalized, Vector3.down);

        // 일정 각도 이상 아래로 향하면 곡면 뒤에서 쏘는 것이므로 무시
        if (verticalDir > 0.46f)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
