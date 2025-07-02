using System;
using UnityEngine;
using UnityEngine.UI;

public class ExitDialogue : MonoBehaviour
{
    public GameObject quizUI;
    private bool _quizResult;
    private int _smokeTouchedCount;

    private DialoguePlayer _dialoguePlayer;
    private ScoreManager _scoreManager;
    private FixedViewCanvasController _fvCanvasController;

    private void Start()
    {
        _dialoguePlayer = FindObjectOfType<DialoguePlayer>();
        _scoreManager = FindObjectOfType<ScoreManager>();
        _fvCanvasController = FindObjectOfType<FixedViewCanvasController>();

        // 버튼 이벤트 달아주기
        quizUI.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(OnSelectRightAnswer);
        quizUI.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(OnSelectWrongAnswer);
        Invoke("OnStartExitScene", 1f);
    }

    private void OnStartExitScene()
    {
        _dialoguePlayer.PlayWithText("EXIT_001", UIType.Sobaek);
    }

    public void OnBeforeStartShootingTrack()
    {
        _dialoguePlayer.PlayWithText("EXIT_002", UIType.Sobaek);
    }

    public void OnStartSmokePlace()
    {
        _dialoguePlayer.PlayWithText("EXIT_015", UIType.Sobaek);
    }

    public void OnSelectRightAnswer()
    {
        HideQuizUI();
        _dialoguePlayer.PlayWithText("EXIT_003", UIType.Sobaek);
        _quizResult = true;
        _scoreManager.SetScore(ScoreType.Elevator, CalculateQuizScore());
        _scoreManager.SetScore(ScoreType.DaTaewoori, 25);
    }

    public void OnSelectWrongAnswer()
    {
        HideQuizUI();
        _dialoguePlayer.PlayWithText("EXIT_004", UIType.Sobaek);
        _scoreManager.SetScore(ScoreType.Elevator, CalculateQuizScore());
        _scoreManager.SetScore(ScoreType.DaTaewoori, 25);
    }

    public void ShowQuizUI()
    {
        quizUI.SetActive(true);
    }

    public void HideQuizUI()
    {
        quizUI.SetActive(false);
    }

    private int CalculateQuizScore() => _quizResult ? 25 : 15;
    private int CalculateSmokeScore() => _smokeTouchedCount < 2 ? 25 : 15;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Smoke"))
        {
            _fvCanvasController.TurnWarningSign(true);
            _smokeTouchedCount++;
        };
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("Smoke"))
        {
            _fvCanvasController.TurnWarningSign(false);
        };
    }

    public void SendSmokeScore()
    {
        int score = CalculateSmokeScore();
        _scoreManager.SetScore(ScoreType.Smoke, score);
    }
}
