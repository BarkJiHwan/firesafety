using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public enum GamePhase
    {
        Waiting,      // 0~10초
        Prevention,   // 10~70초
        Fire,         // 70~190초
        Fever,       // 190초~
        leaveDangerArea
    }

    [SerializeField] private float _gameTimer = 0f;
    [SerializeField] private GamePhase _currentPhase = GamePhase.Waiting;

    [SerializeField] private bool _isGameStart = true;

    public float GameTimer => _gameTimer;
    public GamePhase CurrentPhase => _currentPhase;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        _currentPhase = GamePhase.Waiting;
    }
    private void Start()
    {
        //GameStartBtn();
    }

    private void Update()
    {
        if (_isGameStart)
        {
            _gameTimer += Time.deltaTime;
            UpdateGamePhase();
        }
    }

    private void UpdateGamePhase()
    {
        // 시간에 따라 페이즈 자동 전환
        if (_gameTimer < 10f)
        {
            _currentPhase = GamePhase.Waiting;
        }
        else if (_gameTimer < 70f)
        {
            if (_currentPhase != GamePhase.Prevention)
            {
                _currentPhase = GamePhase.Prevention;
            }
        }
        else if (_gameTimer < 190f)
        {
            if (_currentPhase != GamePhase.Fire)
            {
                _currentPhase = GamePhase.Fire;
            }
        }
        else if(_gameTimer < 250f)
        {
            if (_currentPhase != GamePhase.Fever)
            {
                _currentPhase = GamePhase.Fever;
            }
        }
        else
        {
            if(_currentPhase != GamePhase.leaveDangerArea)
            {
                _currentPhase = GamePhase.leaveDangerArea;
                _isGameStart = false; //스타트 멈춤
                Debug.Log("일단 게임종료 임");
            }
        }
    }

    public void GameStartBtn()
    {
        _isGameStart = true;
    }
}
