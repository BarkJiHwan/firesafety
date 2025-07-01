using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Waiting,      // 1초
    Prevention,   // 1~61초
    FireWaiting,      // 61~62초
    Fire,         // 62~182초
    Fever,       // 182 ~ 242초
    LeaveDangerArea //242초 게임 끝
}
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.Log("인스턴스 없음");
            }
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }

    [SerializeField] private bool _isGameStart = false;
    [SerializeField] private List<GamePhaseInfo> _phases;
    private Coroutine _gameTimerCoroutine;

    [field: SerializeField]
    public float GameTimer { get; private set; } = 0f;

    public GamePhase CurrentPhase { get; set; } = GamePhase.Waiting;

    // 차연우 수정
    public GamePhase _currentPhase;
    public GamePhase NowPhase
    {
        get => _currentPhase;
        set
        {
            if (_currentPhase != value)
            {
                Debug.Log("_currentPhase : " + _currentPhase);
                _currentPhase = value;
                OnPhaseChanged?.Invoke(_currentPhase);
            }
        }
    }

    public bool IsGameStart { get => _isGameStart; set => _isGameStart = value; }

    public event Action OnGameEnd;
    public event Action<GamePhase> OnPhaseChanged;

    private GamePhaseInfo waiting;
    private GamePhaseInfo prevention;
    private GamePhaseInfo fireWaiting;
    private GamePhaseInfo fire;
    private GamePhaseInfo fever;
    private GamePhaseInfo leaveDangerArea;

    private DialogueLoader _dialogueLoader;
    private DialoguePlayer _dialoguePlayer;

    /* 일시정지 할때 추가 */
    private bool _isPausing;
    public event Action onGamePause;
    public event Action onGameResume;

    public bool IsPausing { get; private set; }
    public float[] StartTime { get; private set; }

    private void Awake()
    {
        _dialoguePlayer = FindObjectOfType<DialoguePlayer>();
        _dialogueLoader = _dialoguePlayer.GetComponent<DialogueLoader>();

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    private void Start()
    {
        CachingPhaseList();
        GameStart();

        // Time 세팅
        SetStartTime();
    }

    private IEnumerator GameTimerRoutine()
    {
        yield return new WaitUntil(() => IsGameStart);
        while (IsGameStart)
        {
            if (!_isPausing)
            {
                GameTimer += Time.deltaTime;
            }
            UpdateGamePhaseCor(GameTimer);
            yield return null;
        }
    }
    public void StopGame()
    {
        IsGameStart = false;
        if (_gameTimerCoroutine != null)
        {
            StopCoroutine(_gameTimerCoroutine);
            _gameTimerCoroutine = null;
        }
    }
    private void UpdateGamePhaseCor(float timer)
    {
        GamePhaseInfo now = GetPhase(timer);

        if (now.Phase != CurrentPhase)
        {
            CurrentPhase = now.Phase;
            //_currentPhase = now.Phase;
            NowPhase = now.Phase;

            if (CurrentPhase == GamePhase.FireWaiting)
            {
                PauseGameTimer();
                _dialoguePlayer.onFinishDialogue += ResumeGameTimer;
                _dialoguePlayer.PlayWithTexts(new []{"Sobak_009", "Sobak_011"}, UIType.Sobaek);
            }
            // GameManager.cs - UpdateGamePhaseCor 메서드에서
            if (CurrentPhase == GamePhase.LeaveDangerArea)
            {
                Debug.Log("게임 종료! 점수 계산 시작");
                TaewooriPoolManager.Instance?.CalculateFinalScores(); // 변경된 메서드 호출
                OnGameEnd?.Invoke();
            }
        }
    }
    public void SetGameTimer(float time)
    {
        GameTimer = time;
    }

    /* 모두 레디 후 시작, 대화창 끝나면 게임 진짜 시작 하도록 이벤트 전달 */
    public void GameStartWhenAllReady()
    {
        IsGameStart = true;
        PauseGameTimer();
        _dialogueLoader.LoadSobaekData();
        _dialoguePlayer.onFinishDialogue += ResumeGameTimer;
        _dialoguePlayer.PlayWithTexts(new []{"Sobak_001", "Sobak_002", "Sobak_003", "Sobak_004"}, UIType.Sobaek);
    }

    private void PauseGameTimer()
    {
        Debug.Log("Pausing");
        _isPausing = true;
        onGamePause?.Invoke();
    }

    private void ResumeGameTimer()
    {
        Debug.Log("Resuming");
        _isPausing = false;
        _dialoguePlayer.onFinishDialogue -= ResumeGameTimer;
        onGameResume?.Invoke();
    }

    public void GameStart()
    {
        _currentPhase = GamePhase.Waiting;
        StopGame();
        GameTimer = 0f;
        _gameTimerCoroutine = StartCoroutine(GameTimerRoutine());
    }
    public void ResetGameTimer()
    {
        //_isGameStart = true;
        GameTimer = 0f;
        //CHM 태우리 킬카운트 리셋
        TaewooriPoolManager.Instance?.ResetKillCounts();
        TaewooriPoolManager.Instance?.CleanupAllResources();
    }

    private void CachingPhaseList()
    {
        foreach (GamePhaseInfo phaseInfo in _phases)
        {
            if (phaseInfo.Phase == GamePhase.Waiting)
            {
                waiting = phaseInfo;
            }
            else if (phaseInfo.Phase == GamePhase.Prevention)
            {
                prevention = phaseInfo;
            }
            else if (phaseInfo.Phase == GamePhase.FireWaiting)
            {
                fireWaiting = phaseInfo;
            }
            else if (phaseInfo.Phase == GamePhase.Fire)
            {
                fire = phaseInfo;
            }
            else if (phaseInfo.Phase == GamePhase.Fever)
            {
                fever = phaseInfo;
            }
            else if (phaseInfo.Phase == GamePhase.LeaveDangerArea)
            {
                leaveDangerArea = phaseInfo;
            }
        }
    }

    private GamePhaseInfo GetPhase(float gameTimer)
    {
        GamePhaseInfo now;

        if (gameTimer >= leaveDangerArea.StartTime)
        {
            now = leaveDangerArea;
        }
        else if (gameTimer >= fever.StartTime)
        {
            now =  fever;
        }
        else if (gameTimer >= fire.StartTime)
        {
            now =  fire;
        }
        else if (gameTimer >= fireWaiting.StartTime)
        {
            now = fireWaiting;
        }
        else if (gameTimer >= prevention.StartTime)
        {
            now =  prevention;
        }
        else
        {
            now = waiting;
        }

        return now;
    }

    [Serializable]
    public class GamePhaseInfo
    {
        public GamePhase Phase;
        public float StartTime;
    }

    // Phases StartTime 받아오기
    void SetStartTime()
    {
        StartTime = new float[_phases.Count];

        for(int i=0; i<_phases.Count; i++)
        {
            StartTime[i] = _phases[i].StartTime;
        }
    }
}
