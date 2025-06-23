using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System;
using System.Linq;

public class PlayerTutorial : MonoBehaviourPun
{
    private TutorialData _myData;
    private int _currentPhase = 1;
    private int _playerIndex;
    private GameObject _zone;
    private GameObject _currentMonster;
    private GameObject _extinguisher;
    private FirePreventable _preventable;
    private Coroutine _tutorialCor;
    private Coroutine _tutorialTimerCor;

    private DialogueLoader _dialogueLoader;
    private TutorialAudioPlayer _tutorialAudioPlayer;

    bool isMaterialOn = false;
    private RoomMgr _roomMgr;

    // CYW_이벤트 발생
    public event Action<int> OnStartArrow;
    public event Action<GameObject> OnObjectUI;
    public event Action OnCompleteSign;
    public event Action OnFinishTutorial;

    public ArrowController arrowCtrl { get; set; }


    void Start()
    {
        if (photonView == null || !photonView.IsMine)
            return;
        StartCoroutine(CountdownRoutine());

        GameObject dialogue = null;
        if (_dialogueLoader == null)
        {
            _dialogueLoader = FindObjectOfType<DialogueLoader>();
            dialogue = _dialogueLoader.gameObject;
        }
        if (_tutorialAudioPlayer == null)
        {
            _tutorialAudioPlayer = dialogue.GetComponent<TutorialAudioPlayer>();
        }

        _roomMgr = FindObjectOfType<RoomMgr>();
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
        yield return new WaitForSeconds(timer);
        _playerIndex = TutorialDataMgr.Instance.PlayerNumber;
        _myData = TutorialDataMgr.Instance.GetPlayerData(_playerIndex);
        SetTutorialPhase();
        ObjectActiveFalse();
        _tutorialTimerCor = StartCoroutine(TutorialTimer());
    }
    private IEnumerator TutorialTimer()
    {
        _tutorialCor = StartCoroutine(TutorialRoutine());
        float timer = 90f;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        StartCoroutine(TutorialTimeOver());
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
        _tutorialAudioPlayer.PlayVoiceWithText("TUT_001", UIType.Narration);
        yield return new WaitUntil(() => !_tutorialAudioPlayer._tutoAudio.isPlaying);
        OnStartArrow?.Invoke(_playerIndex);
        TutorialDataMgr.Instance.IsStartTutorial = true;
        _tutorialAudioPlayer.PlayVoiceWithText("TUT_002", UIType.Narration);

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
            _tutorialAudioPlayer.TutorialAudioWithTextStop();
            _zone.SetActive(false);
            Debug.Log("이동 튜토리얼 완료");
            arrowCtrl.gameObject.SetActive(false);
        };
        yield return new WaitUntil(() => completed);
        _tutorialAudioPlayer.PlayVoiceWithText("TUT_003", UIType.Narration);
        yield return new WaitUntil(() => !_tutorialAudioPlayer._tutoAudio.isPlaying);
    }

    private IEnumerator someCorutine()
    {
        yield return null;
    }

    // 2. 화재예방 패이즈
    private IEnumerator HandleInteractionPhase()
    {
        _tutorialAudioPlayer.PlayVoiceWithText("TUT_004", UIType.Narration);

        Debug.Log("화재예방 튜토리얼 시작");
        var interactObj = TutorialDataMgr.Instance.GetInteractObject(_playerIndex);
        _preventable = interactObj.GetComponent<FirePreventable>();
        _preventable.OnHaveToPrevented += _preventable.OnSetPreventMaterialsOn;
        _preventable.TriggerPreventObejct(true);
        OnObjectUI?.Invoke(interactObj);

        _preventable.SetFirePreventionPending();
        var interactable = interactObj.GetComponent<XRSimpleInteractable>();
        bool completed = false;
        interactable.selectEntered.AddListener(tutorialSelect =>
        {
            _tutorialAudioPlayer.TutorialAudioWithTextStop();
            completed = true;
            Debug.Log("화재예방 튜토리얼 완료");
            _preventable.OnFirePreventionComplete();
            // 이벤트 실행
            _preventable.OnAlreadyPrevented += _preventable.OnSetPreventMaterialsOff;
            _preventable.TriggerPreventObejct(false);
            // 완료했다는 표시 생성
            OnCompleteSign?.Invoke();
            isMaterialOn = true;
        });
        StartCoroutine(MakeMaterialMoreBright());
        yield return new WaitUntil(() => completed);
        _tutorialAudioPlayer.PlayVoiceWithText("TUT_005", UIType.Narration);
        yield return new WaitUntil(() => !_tutorialAudioPlayer._tutoAudio.isPlaying);
        interactable.selectEntered.RemoveAllListeners();
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
            if (_preventable.GetHighlightProperty() == true)
            {
                _preventable.SetHighlight(t);
            }
            yield return null;
        }
    }

    // 3. 전투 페이즈
    private IEnumerator HandleCombatPhase()
    {
        _preventable.SetActiveOut();
        // 소화기 메테리얼 수정
        MakeExtinguisherMaterial(_extinguisher);
        _tutorialAudioPlayer.PlayVoiceWithText("TUT_006", UIType.Narration);

        _currentMonster.SetActive(true);
        _extinguisher.SetActive(true);

        // 소화기 위에 UI 나오게 하기
        OnObjectUI?.Invoke(_extinguisher);

        // 2. 몬스터 체력 컴포넌트 참조
        var tutorial = _currentMonster.GetComponent<TaewooriTutorial>();
        if (tutorial == null)
        {
            tutorial = _currentMonster.AddComponent<TaewooriTutorial>();
        }

        // 3. 체력 0 될 때까지 폴링
        yield return new WaitUntil(() => tutorial.CurrentHealth <= 0);//CHM수정
        _currentMonster.SetActive(false); //태우리 끄기
        _tutorialAudioPlayer.TutorialAudioWithTextStop();
        _tutorialAudioPlayer.PlayVoiceWithText("TUT_007", UIType.Narration);

        yield return new WaitUntil(() => TutorialDataMgr.Instance.IsTriggerSupply);
        _tutorialAudioPlayer.TutorialAudioWithTextStop();

        // 완료했다는 표시 생성
        OnCompleteSign?.Invoke();

        StopCoroutine(_tutorialTimerCor);
        //준비 완료
        _tutorialAudioPlayer.PlayVoiceWithText("TUT_008", UIType.Narration);
        yield return new WaitUntil(() => !_tutorialAudioPlayer._tutoAudio.isPlaying);
        Hashtable props = new Hashtable() { { "IsReady", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        if (PhotonNetwork.PlayerList.Count() > 1 && !_roomMgr.isAllPlayersReady())
        {
            _tutorialAudioPlayer.PlayVoiceWithText("TUT_009", UIType.Narration);
        }
        yield return new WaitUntil(() => GameManager.Instance.IsGameStart);
        ObjectActiveFalse(); //모든 튜토리얼 오브젝트 끄기
        DestroyTutorialObject();
        // 튜토리얼 끝났을때 이벤트 실행
        OnFinishTutorial?.Invoke();
        StopAllCoroutines();
    }

    private IEnumerator TutorialTimeOver()
    {
        StopCoroutine(_tutorialCor);
        ObjectActiveFalse();
        DestroyTutorialObject();
        if (_preventable != null)
        {
            _preventable.SetActiveOut();
        }
        Debug.Log("으휴! 이것도 못해?!");

        // 메테리얼 끄기
        if (isMaterialOn == true)
        {
            _preventable.OnAlreadyPrevented += _preventable.OnSetPreventMaterialsOff;
            _preventable.TriggerPreventObejct(false);
        }

        if (arrowCtrl.gameObject.activeSelf == true)
        {
            arrowCtrl.gameObject.SetActive(false);
        }

        // 튜토리얼 끝났을때 이벤트 실행
        OnFinishTutorial?.Invoke();

        _tutorialAudioPlayer.PlayVoiceWithText("TUT_011", UIType.Narration);
        yield return new WaitUntil(() => !_tutorialAudioPlayer._tutoAudio.isPlaying);
        Hashtable props = new Hashtable() { { "IsReady", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        StopAllCoroutines();
    }

    void MakeExtinguisherMaterial(GameObject obj)
    {
        // 소화기 메테리얼 작업
        Material[] mats = new Material[2];
        mats[0] = Resources.Load<Material>("Materials/OutlineMat");
        mats[1] = Resources.Load<Material>("Materials/OriginMat");
        mats[1].SetTexture("_PreventTexture", Resources.Load<Texture2D>("Materials/ExtinguisherColor"));
        mats[1].SetFloat("_isNearPlayer", 1f);
        Renderer rend = obj.GetComponent<Renderer>();
        rend.materials = mats;
    }
}
