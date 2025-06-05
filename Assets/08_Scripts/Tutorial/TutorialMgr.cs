using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;

public class TutorialMgr : MonoBehaviourPun
{
    [Header("플레이어별 튜토리얼 데이터 (0~5번까지)")]

    private TutorialData _myData;
    private int _currentPhase = 1;
    private int _playerIndex;
    private GameObject _currentMonster;

    void Start()
    {
        if (!photonView.IsMine)
            return;

        _playerIndex = PhotonNetwork.LocalPlayer.ActorNumber;
        _myData = TutorialDataMgr.Instance.GetPlayerData(
            _playerIndex
        );

        StartCoroutine(TutorialRoutine());
    }

    private System.Collections.IEnumerator TutorialRoutine()
    {
        while (_currentPhase <= 3)
        {
            switch (_currentPhase)
            {
                case 1:
                    yield return HandleMovementPhase();
                    break;
                case 2:
                    yield return HandleInteractionPhase();
                    break;
                case 3:
                    yield return HandleCombatPhase();
                    break;
            }
            _currentPhase++;
        }
    }

    // 1. 이동 페이즈
    private IEnumerator HandleMovementPhase()
    {
        GameObject zone = Instantiate(_myData.moveZonePrefab);
        zone.SetActive(true);

        bool completed = false;
        var trigger = zone.GetComponent<ZoneTrigger>();
        if (trigger == null)
            trigger = zone.AddComponent<ZoneTrigger>();

        trigger.onEnter += () => {
            completed = true;
            Destroy(zone);
            Debug.Log("이동 완료!");
        };

        yield return new WaitUntil(() => completed);
    }

    // 2. 상호작용 페이즈
    private IEnumerator HandleInteractionPhase()
    {
        var interactObj = TutorialDataMgr.Instance.GetInteractObject(_playerIndex);
        interactObj.SetActive(true);

        var interactable = interactObj.GetComponent<XRSimpleInteractable>();
        bool completed = false;
        interactable.selectEntered.AddListener(_ =>
        {
            completed = true;
            interactObj.SetActive(false);
            Debug.Log("상호작용 성공!");
        });

        yield return new WaitUntil(() => completed);
    }

    // 3. 전투 페이즈
    private IEnumerator HandleCombatPhase()
    {
        _currentMonster = Instantiate(_myData.teawooriPrefab);
        GameObject extinguisher = Instantiate(_myData.supplyPrefab);

        bool completed = false;
        var extinguisherScript = extinguisher.GetComponent<FireExtinguisher>();
        if (extinguisherScript == null)
            extinguisherScript = extinguisher.AddComponent<FireExtinguisher>();
        extinguisherScript.OnUse += () => {
            if (_currentMonster != null)
                Destroy(_currentMonster);
            completed = true;
            Debug.Log("튜토리얼 완료");
        };

        yield return new WaitUntil(() => completed);
    }
}
