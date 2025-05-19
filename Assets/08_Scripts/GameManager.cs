using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public interface IPhaseProvider
//{
//    GameManager.GamePhase CurrentPhase { get; }
//    float GameTimer { get; }
//}

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
        Burning       // 190초~
    }

    [SerializeField] private float _gameTimer = 0f;
    [SerializeField] private GamePhase _currentPhase = GamePhase.Waiting;

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
    }

    private void Start()
    {
        Debug.Log("10초 후 게임이 시작됩니다...");
    }

    private void Update()
    {
        _gameTimer += Time.deltaTime;
        UpdateGamePhase();
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
                Debug.Log(">> 예방 페이즈 진입");
            _currentPhase = GamePhase.Prevention;
        }
        else if (_gameTimer < 190f)
        {
            if (_currentPhase != GamePhase.Fire)
                Debug.Log(">> 화재 페이즈 진입");
            _currentPhase = GamePhase.Fire;
        }
        else
        {
            if (_currentPhase != GamePhase.Burning)
                Debug.Log(">> 버닝 페이즈 진입");
            _currentPhase = GamePhase.Burning;
        }
    }
}
