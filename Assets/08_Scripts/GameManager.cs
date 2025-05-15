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
    [SerializeField] private bool _isGameStart;
    [SerializeField] private bool _isGameStop;

    [SerializeField] private float _gameTimer;

    public bool IsGameStart { get => _isGameStart; set => _isGameStart = value; }
    public bool IsGameStop { get => _isGameStop; set => _isGameStop = value; }
    public float GameTimer { get => _gameTimer; set => _gameTimer = value; }


    void Start()
    {
        Debug.Log("10초 후 게임이 시작 됩니다.");
    }

    void Update()
    {
        GameTimer += Time.deltaTime;
        if(GameTimer >= 10)
        {
            IsGameStart = true;
        }
        if(IsGameStart)
        {
            Debug.Log("게임 시작");
            Debug.Log("예방 페이즈 진입");
            _isGameStart = false;
        }
    }
}
