using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Phase2_TapWater : MonoBehaviour
{
    [SerializeField] private GameObject _waterRunning;
    [SerializeField] private GameObject _waterStag;
    [SerializeField] private bool _turnOn = false;
    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log("물켜는 거 부름");
        // 누른 손의 인터랙터 정보 가져오기
        XRBaseInteractor interactor = args.interactorObject as XRBaseInteractor;

        var type = interactor.GetComponent<HandIdentifier>();
        if (interactor == null)
        {
            Debug.LogWarning("interactor 없는데용");
            return;
        }
        var handType = type.handType;
        var wasGotTowelWet = FindObjectOfType<Phase2InteractManager>().gotWet;
        // 매니저에게 전달
        if (Phase2ObjectManager.Instance != null && !wasGotTowelWet && !_turnOn)
        {
            TurnOnWater();
        }
        else if (Phase2ObjectManager.Instance != null && !wasGotTowelWet && _turnOn)
        {
            Phase2ObjectManager.Instance.WettingTowel(handType);
            _waterRunning.SetActive(false);
            _waterStag.SetActive(true);
        }
    }
    private void TurnOnWater()
    {
        _waterRunning.SetActive(true);
        _turnOn = true;
    }
}
