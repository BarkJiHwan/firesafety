using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Waiting,      // 0~10초
    Prevention,   // 10~70초
    Fire,         // 70~190초
    Fever,       // 190초~
    LeaveDangerArea
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
    private int _currentPhaseIndex = -1;
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

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        StopGame();
        GameTimer = 0f;
        _currentPhaseIndex = -1;
        _gameTimerCoroutine = StartCoroutine(GameTimerRoutine());
    }

    private IEnumerator GameTimerRoutine()
    {
        yield return new WaitUntil(() => IsGameStart);
        while (IsGameStart)
        {
            GameTimer += Time.deltaTime;
            UpdateGamePhaseCor();
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
    private void UpdateGamePhaseCor()
    {
        for (int i = _phases.Count - 1; i >= 0; i--)
        {
            if (GameTimer >= _phases[i].StartTime)
            {
                if (_currentPhaseIndex != i)
                {
                    _currentPhaseIndex = i;
                    CurrentPhase = _phases[i].Phase;
                    NowPhase = _phases[i].Phase;
                    _phases[i].OnEnterPhase?.Invoke();

                    if (CurrentPhase == GamePhase.LeaveDangerArea)
                        OnGameEnd?.Invoke();
                }
                break;
            }
        }
    }
    public void SetGameTimer(float time)
    {
        GameTimer = time;
    }

    public void GameStartBtn()
    {
        IsGameStart = true;
    }
    public void GmaeOver()
    {
        IsGameStart = false;
    }
    public void ResetGameTimer()
    {
        //_isGameStart = true;
        GameTimer = 0f;
        //CHM 태우리 생존시간 리셋
        TaewooriPoolManager.Instance?.ResetSurvivalTracking();
    }

    [Serializable]
    public class GamePhaseInfo
    {
        public GamePhase Phase;
        public float StartTime;
        public Action OnEnterPhase;
    }
}
