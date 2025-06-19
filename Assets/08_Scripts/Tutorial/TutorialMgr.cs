using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using System;

public class TutorialMgr : MonoBehaviourPun
{
    private TutorialData _myData;
    private int _currentPhase = 1;
    private int _playerIndex;
    private GameObject _zone;
    private GameObject _currentMonster;
    private GameObject _extinguisher;
    private FirePreventable _preventable;
    private Coroutine _countdownCoroutine;

    DialogueLoader dialogueLoader;
    DialoguePlayer dialoguePlayer;

    void Start()
    {
        if (!photonView.IsMine)
            return;

        _playerIndex = PhotonNetwork.PlayerList.ToList().IndexOf(PhotonNetwork.LocalPlayer);
        _myData = TutorialDataMgr.Instance.GetPlayerData(_playerIndex);
        SetTutorialPhase();
        ObjectActiveFalse();
        _countdownCoroutine = StartCoroutine(CountdownRoutine());

        GameObject dialogue = null;
        if(dialogueLoader == null)
        {
            dialogueLoader = FindObjectOfType<DialogueLoader>();
            dialogue = dialogueLoader.gameObject;
        }
        if(dialoguePlayer == null)
        {
            dialoguePlayer = dialogue.GetComponent<DialoguePlayer>();
        }
    }
    public void SetTutorialPhase()
    {
        _zone = Instantiate(_myData.moveZonePrefab);
        _zone.transform.position = _myData.moveZoneOffset;
        var obj = TutorialDataMgr.Instance.InteractObjects[_playerIndex].GetComponent<FireObjScript>();
        _currentMonster = Instantiate(_myData.teawooriPrefab, obj.TaewooriPos(), obj.TaewooriRotation());
        _extinguisher = Instantiate(_myData.supplyPrefab, _myData.supplyOffset, _myData.supplyRotation);
    }
    private void ObjectActiveFalse()
    {
        _zone.SetActive(false);
        _currentMonster.SetActive(false);
        _extinguisher.SetActive(false);
    }
    private void DestroyTutorialObject()
    {
        Destroy(_zone);
        Destroy(_currentMonster);
        Destroy(_extinguisher);
    }
    private IEnumerator CountdownRoutine()
    {
        //약 3초 뒤 튜토리얼 시작
        float timer = 3f;
        Debug.Log("3초 뒤 튜토리얼을 시작합니다.");
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        Debug.Log("튜토리얼 시작");
        StartCoroutine(TutorialRoutine());
        StartCoroutine(StopTutoria());
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
        //사운드가 끝나면 시작합니다.
        //이 부분에 Tutorial_NAR_001이 종료 될 때 까지 기다렸다 시작하면 됨
        Debug.Log("이동 튜토리얼 시작");
        //튜토리얼 시작 트리거
        TutorialDataMgr.Instance.IsStartTutorial = true;

        _zone.SetActive(true);
        //이 부분에서 Tutorial_NAR_002 실행하면 됨
        bool completed = false;
        var trigger = _zone.GetComponent<ZoneTrigger>();
        if (trigger == null)
        {
            trigger = _zone.AddComponent<ZoneTrigger>();
        }

        trigger.onEnter += () =>
        {
            //TUT_SND_001 미션 클리어 사운드 실행
            //Tutorial_NAR_002번 나레이션 종료
            completed = true;
            _zone.SetActive(false);
            Debug.Log("이동 튜토리얼 완료");
        };

        yield return new WaitUntil(() => completed);
        //Tutorial_NAR_003번 나레이션 실행 : 잘했어요!
    }

    // 2. 화재예방 패이즈
    private IEnumerator HandleInteractionPhase()
    {
        //Tutorial_NAR_003번 나레이션이 끝난 것을 확인하고
        //Tutorial_NAR_004번 나레이션 실행
        Debug.Log("화재예방 튜토리얼 시작");
        var interactObj = TutorialDataMgr.Instance.GetInteractObject(_playerIndex);
        _preventable = interactObj.GetComponent<FirePreventable>();
        // 이벤트 실행
        _preventable.OnHaveToPrevented += _preventable.OnSetPreventMaterialsOn;
        _preventable.TriggerPreventObejct(true);

        _preventable.SetFirePreventionPending();
        var interactable = interactObj.GetComponent<XRSimpleInteractable>();
        bool completed = false;
        interactable.selectEntered.AddListener(tutorialSelect =>
        {
            //Tutorial_NAR_004번 나레이션 종료
            //TUT_SND_001 미션 클리어 사운드 실행
            completed = true;
            Debug.Log("화재예방 튜토리얼 완료");
            //Tutorial_NAR_005번 나레이션 실행 : 멋져요!
            _preventable.OnFirePreventionComplete();
            // 이벤트 실행
            _preventable.OnAlreadyPrevented += _preventable.OnSetPreventMaterialsOff;
            _preventable.TriggerPreventObejct(false);
        });
        StartCoroutine(MakeMaterialMoreBright());

        yield return new WaitUntil(() => completed);
        interactable.selectEntered.RemoveAllListeners();
        _preventable.SetActiveOut();
    }

    IEnumerator MakeMaterialMoreBright()
    {
        var interactObj = TutorialDataMgr.Instance.GetInteractObject(_playerIndex);

        GameObject player = FindObjectOfType<PlayerComponents>().gameObject;
        player = player.GetComponentInChildren<PlayerInteractor>().gameObject;
        Debug.Log(player.name);

        while (_currentPhase == 2)
        {
            // 플레이어가 가까워질수록 내 Material _RimPower -시켜야 함 2->-0.2
            float distance = Vector3.Distance(_preventable.transform.position, player.transform.position);
            // 빛을 더 밝게 빛나기 위해서 * 2 했음
            float t = (1 - Mathf.Clamp01(distance / 2f)) * 2;
            if(_preventable.GetHighlightProperty() == true)
            {
                _preventable.SetHighlight(t);
            }
            yield return null;
        }
    }

    // 3. 전투 페이즈
    private IEnumerator HandleCombatPhase()
    {
        //Tutorial_NAR_005번 나레이션이 끝난 것을 확인하고
        //Tutorial_NAR_006번 나레이션 실행 : 마지막으로 소화기를 사용해보세요 어쩌구....
        Debug.Log("전투 튜토리얼 시작");
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
        Debug.Log("태우리 죽임");
        _currentMonster.SetActive(false); //태우리 끄기
        //Tutorial_NAR_006번 나레이션이 켜져 있으면 종료
        //Tutorial_NAR_007번 나레이션 실행 : 소화기를 다쓰면 바꿔라
        //태우리 처치 완료
        //Tutorial_NAR_007번 나레이션 종료

        Debug.Log("소화기를 클릭하세요.");
        //소화기 상호작용 완료까지 대기하기.
        yield return new WaitUntil(() => TutorialDataMgr.Instance.IsTriggerSupply);
        //Tutorial_NAR_008번 나레이션 실행 : 잘했다 모두 끝났다.
        //TUT_SND_001 미션 클리어 사운드 실행
        Debug.Log("소화기 상호작용 완료");

        //준비 완료
        Debug.Log("모든 튜토리얼 완료");
        TutorialDataMgr.Instance.StopTutorialRoutine();
        Debug.Log("방장님 저 튜토리얼 끝났습니다.");
        Hashtable props = new Hashtable() { { "IsReady", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        //8번 나레이션이 종료 될때 까지 잠깐 대기
        if (PhotonNetwork.PlayerList.Count() > 1)
        {
            //8번 나래이션 끝나면 9번 나래이션 실행 : 아직 안끝난 친구를 기다려!
            Debug.Log("다른 사람이 튜토리얼 진행중 입니다. 기다리세요");
        }
        //Tutorial_NAR_010번 나레이션 실행 : 이제 게임 할거니까 잠깐 기다려~
        yield return new WaitUntil(() => GameManager.Instance.IsGameStart);
        Debug.Log("곧 게임 시작합니다.");
        ObjectActiveFalse(); //모든 튜토리얼 오브젝트 끄기
        DestroyTutorialObject();
        StopAllCoroutines();
    }

    private IEnumerator StopTutoria()
    {
        yield return new WaitUntil(() => TutorialDataMgr.Instance.IsTutorialFailed);
        StopCoroutine(_countdownCoroutine);
        ObjectActiveFalse();
        DestroyTutorialObject();
        if (_preventable != null)
        {
            _preventable.SetActiveOut();
        }
        Debug.Log("으휴! 이것도 못해?!");
        Hashtable props = new Hashtable() { { "IsReady", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // 메테리얼 끄기
        var interactObj = TutorialDataMgr.Instance.GetInteractObject(_playerIndex);
        _preventable.OnAlreadyPrevented += _preventable.OnSetPreventMaterialsOff;
        _preventable.TriggerPreventObejct(false);

        //11번 나레이션 실행 : 아쉽지만 어쩌구...
        //나레이션 종료 후 실행하기.
        StopAllCoroutines();
    }
}
