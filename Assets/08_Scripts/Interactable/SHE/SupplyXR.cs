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

        // 왼손 / 오른손 구분 (이름 또는 태그 등으로 판별)
        EHandType handType = EHandType.RightHand; // 기본값

        string interactorName = interactor.name.ToLower();
        if (interactorName.Contains("left"))
        {
            handType = EHandType.LeftHand;
        }
        else if (interactorName.Contains("right"))
        {
            handType = EHandType.RightHand;
        }

        Debug.Log($"보급소 눌림: {handType}");

        // 매니저에게 전달
        if (SupplyManager.Instance != null)
        {
            SupplyManager.Instance.Supply(handType);
        }
    }
}
