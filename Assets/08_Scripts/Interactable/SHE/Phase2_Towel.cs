using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Phase2_Towel : MonoBehaviour
{
    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log("수건 보급 부름");
        XRBaseInteractor interactor = args.interactorObject as XRBaseInteractor;

        var type = interactor.GetComponent<HandIdentifier>();
        if (interactor == null)
        {
            Debug.LogWarning("interactor 없는데용");
            return;
        }
        var handType = type.handType;

        // 매니저에게 전달
        if (Phase2ObjectManager.Instance != null)
        {
            Phase2ObjectManager.Instance.SupplyTowel(handType);
        }
    }
}
