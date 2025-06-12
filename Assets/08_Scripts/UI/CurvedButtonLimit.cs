using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class CurvedButtonLimit : MonoBehaviour
{
    [SerializeField] Transform curvedMeshTrans;
    [SerializeField] XRRayInteractor[] xrRay;

    GameObject curvedMesh;
    Button screenButton;

    ActionBasedController[] controller;

    private void Awake()
    {
        curvedMesh = curvedMeshTrans.gameObject;
        screenButton = GetComponent<Button>();
    }

    private void Start()
    {
        controller = new ActionBasedController[xrRay.Length];
        controller[0] = xrRay[0].transform.parent.GetComponent<ActionBasedController>();
        controller[1] = xrRay[1].transform.parent.GetComponent<ActionBasedController>();
    }

    private void Update()
    {
        if (curvedMeshTrans == null || xrRay == null)
        {
            return;
        }
        bool isRay1In = IsRayBelowCurvedMesh(xrRay[0].transform);
        bool isRay2In = IsRayBelowCurvedMesh(xrRay[1].transform);

        bool isTrigger1 = controller[0].selectAction.action.ReadValue<float>() > 0.5f;
        bool isTrigger2 = controller[1].selectAction.action.ReadValue<float>() > 0.5f;

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

    bool IsRayBelowCurvedMesh(Transform rayTransform)
    {
        // Ray 방향이 아래쪽이면 무효 처리
        float verticalDir = Vector3.Dot(rayTransform.forward.normalized, Vector3.down);
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
