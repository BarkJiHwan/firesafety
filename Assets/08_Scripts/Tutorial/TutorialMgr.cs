using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;

public class TutorialMgr : MonoBehaviourPun
{
    private TutorialData _myData;
    private int _currentPhase = 1;
    private int _playerIndex;
    private GameObject _zone;
    private GameObject _currentMonster;
    private GameObject _extinguisher;
    void Start()
    {
        if (!photonView.IsMine)
            return;

        _playerIndex = PhotonNetwork.LocalPlayer.ActorNumber;
        _myData = TutorialDataMgr.Instance.GetPlayerData(
            _playerIndex
        );
        SetTutorialPhase();
        ObjectActiveFalse();
        StartCoroutine(TutorialRoutine());
    }
    public void SetTutorialPhase()
    {
        _zone = Instantiate(_myData.moveZonePrefab);
        _currentMonster = Instantiate(_myData.teawooriPrefab);
        _extinguisher = Instantiate(_myData.supplyPrefab);
    }
    public void ObjectActiveFalse()
    {
        _zone.SetActive(false);
        _currentMonster.SetActive(false);
        _extinguisher.SetActive(false);
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
        _zone.SetActive(true);

        bool completed = false;
        var trigger = _zone.GetComponent<ZoneTrigger>();
        if (trigger == null)
            trigger = _zone.AddComponent<ZoneTrigger>();

        trigger.onEnter += () =>
        {
            completed = true;
            Destroy(_zone);
            Debug.Log("이동 완료!");
        };

        yield return new WaitUntil(() => completed);
    }

    // 2. 상호작용 페이즈
    private IEnumerator HandleInteractionPhase()
    {
        var interactObj = TutorialDataMgr.Instance.GetInteractObject(_playerIndex);
        var interactable = interactObj.GetComponent<XRSimpleInteractable>();
        bool completed = false;
        interactable.selectEntered.AddListener(_ =>
        {
            completed = true;
            Debug.Log("상호작용 성공!");
        });

        yield return new WaitUntil(() => completed);
    }

    // 3. 전투 페이즈
    private IEnumerator HandleCombatPhase()
    {
        _currentMonster.SetActive(true);
        _extinguisher.SetActive(true);

        // 2. 몬스터 체력 컴포넌트 참조
        var tutorial = _currentMonster.GetComponent<TaewooriTutorial>();
        if (tutorial == null)
        {
            Debug.LogError("몬스터에 TaewooriTutorial(체력) 컴포넌트가 없습니다!");
            yield break;
        }

        // 3. 체력 0 될 때까지 폴링
        yield return new WaitUntil(() => tutorial.currentHealth <= 0);

        // 4. 완료 처리
        _currentMonster.SetActive(false);
        _extinguisher.SetActive(false);
        Debug.Log("튜토리얼 완료");
    }
}
