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

    private Coroutine _countdownCoroutine;
   

    void Start()
    {
        if (!photonView.IsMine)
            return;

        _playerIndex = PhotonNetwork.PlayerList.ToList().IndexOf(PhotonNetwork.LocalPlayer);
        _myData = TutorialDataMgr.Instance.GetPlayerData(_playerIndex);
        SetTutorialPhase();
        ObjectActiveFalse();
        _countdownCoroutine = StartCoroutine(CountdownRoutine());
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
            Destroy(_zone);
            Debug.Log("이동 튜토리얼 완료");
        };

        yield return new WaitUntil(() => completed);
        //Tutorial_NAR_003번 나레이션 실행 : 잘했어요!
    }

    // 2. 상호작용 페이즈
    private IEnumerator HandleInteractionPhase()
    {
        //Tutorial_NAR_003번 나레이션이 끝난 것을 확인하고
        //Tutorial_NAR_004번 나레이션 실행

        var interactObj = TutorialDataMgr.Instance.GetInteractObject(_playerIndex);
        var preventable = interactObj.GetComponent<FirePreventable>();
        preventable.SetFirePreventionPending();
        var interactable = interactObj.GetComponent<XRSimpleInteractable>();
        bool completed = false;
        interactable.selectEntered.AddListener(tutorialSelect =>
        {
            //Tutorial_NAR_004번 나레이션 종료
            //TUT_SND_001 미션 클리어 사운드 실행
            completed = true;
            Debug.Log("상호작용 튜토리얼 완료");
            //Tutorial_NAR_005번 나레이션 실행 : 멋져요!
            preventable.OnFirePreventionComplete();
        });
        yield return new WaitUntil(() => completed);
        interactable.selectEntered.RemoveAllListeners();
        preventable.SetActivePrefab();
    }

    // 3. 전투 페이즈
    private IEnumerator HandleCombatPhase()
    {
        //Tutorial_NAR_005번 나레이션이 끝난 것을 확인하고
        //Tutorial_NAR_006번 나레이션 실행
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
        //Tutorial_NAR_006번 나레이션이 켜져 있으면 종료
        //Tutorial_NAR_007번 나레이션 실행

        //소화기 상호작용 완료까지 대기하기.
        //완료 되면
        //Tutorial_NAR_007번 나레이션 종료
        //TUT_SND_001 미션 클리어 사운드 실행        
        _currentMonster.SetActive(false); //태우리 끄기
        _extinguisher.SetActive(false); //소화기 끄기
        //준비 완료
        Debug.Log("모든 튜토리얼 완료");
        TutorialDataMgr.Instance.StopTutorialRoutine();
        Hashtable props = new Hashtable() { { "IsReady", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        //Tutorial_NAR_008번 나레이션 실행
        //8번 나레이션이 종료 될때 까지 잠깐 대기

        if(PhotonNetwork.PlayerList.Count() > 1)
        {
            //8번 나래이션 끝나면 9번 나래이션 실행 : 아직 안끝난 친구를 기다려!
        }
        //Tutorial_NAR_010번 나레이션 실행
    }

    private IEnumerator StopTutoria()
    {
        yield return new WaitUntil(() => TutorialDataMgr.Instance.IsTutorialFailed);
        StopCoroutine(_countdownCoroutine);
        ObjectActiveFalse();

        Hashtable props = new Hashtable() { { "IsReady", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        //11번 나레이션 실행 : 아쉽지만 어쩌구...
    }
}
