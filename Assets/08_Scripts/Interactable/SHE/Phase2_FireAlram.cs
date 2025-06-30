using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Phase2_FireAlram : MonoBehaviour
{
    [SerializeField] private PlayerSpawner playerSpawner;
    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        bool info = FindObjectOfType<Phase2InteractManager>().IsWear;
        Debug.Log("소화전");
        XRBaseInteractor interactor = args.interactorObject as XRBaseInteractor;

        var type = interactor.GetComponent<HandIdentifier>().handType;
        if (Phase2ObjectManager.Instance != null && info)
        {
            FireAlarm();
            //무기장착
            Phase2ObjectManager.Instance.GrabWeapon(type);
            playerSpawner.StartSobaekCar();
        }
    }
    private void FireAlarm()
    {
        //소리 재생
    }
}
