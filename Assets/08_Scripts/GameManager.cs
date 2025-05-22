using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Waiting,      // 0~10초
    Prevention,   // 10~70초
    Fire,         // 70~190초
    Fever,       // 190초~
    leaveDangerArea
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

    [SerializeField] private bool _isGameStart = true;

    [field: SerializeField]
    public float GameTimer { get; private set; } = 0f;
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Waiting;

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
        //GameStartBtn();
    }

    private void Update()
    {
        if (_isGameStart)
        {
            GameTimer += Time.deltaTime;
            UpdateGamePhase();
        }
    }

    private void UpdateGamePhase()
    {
        // 시간에 따라 페이즈 자동 전환
        if (GameTimer < 10f)
        {
            CurrentPhase = GamePhase.Waiting;
        }
        else if (GameTimer < 70f)
        {
            if (CurrentPhase != GamePhase.Prevention)
            {
                CurrentPhase = GamePhase.Prevention;
            }
        }
        else if (GameTimer < 190f)
        {
            if (CurrentPhase != GamePhase.Fire)
            {
                CurrentPhase = GamePhase.Fire;
            }
        }
        else if(GameTimer < 250f)
        {
            if (CurrentPhase != GamePhase.Fever)
            {
                CurrentPhase = GamePhase.Fever;
            }
        }
        else
        {
            if(CurrentPhase != GamePhase.leaveDangerArea)
            {
                CurrentPhase = GamePhase.leaveDangerArea;
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
