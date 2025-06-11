using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;

public class TutorialMgr : MonoBehaviourPun
{
    private TutorialData _myData;
    private int _currentPhase = 1;
    private int _playerIndex;
    private GameObject _zone;
    private GameObject _currentMonster;
    private GameObject _extinguisher;

    [PunRPC]
    void StartGameCountdown() => StartCoroutine(CountdownRoutine());
    void Start()
    {
        if (!photonView.IsMine)
            return;

        _playerIndex = PhotonNetwork.PlayerList.ToList().IndexOf(PhotonNetwork.LocalPlayer);
        _myData = TutorialDataMgr.Instance.GetPlayerData(_playerIndex);
        SetTutorialPhase();
        ObjectActiveFalse();
        StartGameCountdown();
    }
    public void SetTutorialPhase()
    {
        _zone = Instantiate(_myData.moveZonePrefab);
        _zone.transform.position = _myData.moveZoneOffset;
        var obj = TutorialDataMgr.Instance.InteractObjects[_playerIndex].GetComponent<FireObjScript>();
        _currentMonster = Instantiate(_myData.teawooriPrefab, obj.TaewooriPos(), obj.TaewooriRotation());
        _extinguisher = Instantiate(_myData.supplyPrefab, _myData.supplyOffset, _myData.supplyRotation);
    }
    public void ObjectActiveFalse()
    {
        _zone.SetActive(false);
        _currentMonster.SetActive(false);
        _extinguisher.SetActive(false);
    }
    private IEnumerator CountdownRoutine()
    {
        //약 3초 뒤 튜토리얼 시작
        float timer = 3f;
        while (timer > 0f)
        {
            //UI에 타이머 연결 현제 관련 내용 기획에 없음
            //photonView.RPC("UpdateCountdown", RpcTarget.All, timer);
            timer -= Time.deltaTime;
            yield return null;
        }
        Debug.Log("이제 게임 시작할게요?");
        StartCoroutine(TutorialRoutine());
    }
    private IEnumerator TutorialRoutine()
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
        {
            trigger = _zone.AddComponent<ZoneTrigger>();
        }

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
        float timer = 3f;
        while (timer > 0f)
        {
            // Debug.Log($"다음 튜토리얼 준비까지: {timer:F1}초");
            timer -= Time.deltaTime;
            yield return null;
        }
        var interactObj = TutorialDataMgr.Instance.GetInteractObject(_playerIndex);
        var preventable = interactObj.GetComponent<FirePreventable>();
        preventable.SetFirePreventionPending();
        var interactable = interactObj.GetComponent<XRSimpleInteractable>();
        bool completed = false;
        interactable.selectEntered.AddListener(tutorialSelect =>
        {
            completed = true;
            Debug.Log("상호작용 성공!");
            preventable.OnFirePreventionComplete();
        });

        yield return new WaitUntil(() => completed);
        interactable.selectEntered.RemoveAllListeners();
        timer = 3f;
        while (timer > 0f)
        {
            Debug.Log($"다음 튜토리얼 준비까지: {timer:F1}초");
            timer -= Time.deltaTime;
            yield return null;
        }
        preventable.SetActivePrefab();
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
            tutorial = _currentMonster.AddComponent<TaewooriTutorial>();
        }

        // 3. 체력 0 될 때까지 폴링
        yield return new WaitUntil(() => tutorial.currentHealth <= 0);

        // 4. 완료 처리
        _currentMonster.SetActive(false);
        _extinguisher.SetActive(false);
        TutorialDataMgr.Instance.IsTutorialComplete();
        Debug.Log("튜토리얼 완료");
        Hashtable props = new Hashtable() { { "IsReady", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
}
