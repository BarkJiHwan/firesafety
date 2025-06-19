using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
public class SupplyXR : MonoBehaviour
{
    private XRSimpleInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        if (_interactable == null)
        {
            Debug.LogError("XRSimpleInteractable 없음");
            return;
        }

        // 이벤트 등록
        _interactable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDestroy()
    {
        if (_interactable != null)
        {
            // 이벤트 해제
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // 누른 손의 인터랙터 정보 가져오기
        XRBaseInteractor interactor = args.interactorObject as XRBaseInteractor;

        if (interactor == null)
        {
            Debug.LogWarning("Interactor가 없음");
            return;
        }

        var type = interactor.GetComponent<HandIdentifier>();
        if (interactor == null)
        {
            Debug.LogWarning("interactor 없는데용");
            return;
        }
        EHandType handType = type.handType;

        // 매니저에게 전달
        if (SupplyManager.Instance != null)
        {
            SupplyManager.Instance.Supply(handType);
            Debug.Log("보급을 불러봐");
        }
    }
}
