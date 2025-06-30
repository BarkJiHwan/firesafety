using UnityEngine;

public class ExitDialogue : MonoBehaviour
{
    public GameObject quizUI;
    private bool _quizResult;

    private DialoguePlayer _dialoguePlayer;
    private ScoreManager _scoreManager;

    private void Start()
    {
        _dialoguePlayer = FindObjectOfType<DialoguePlayer>();
        _scoreManager = FindObjectOfType<ScoreManager>();
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

    public void OnTriggerEnter(Collider other)
    {
        // 4층 시작점일때 퀴즈 켜기
        if (other.gameObject.name.Equals("Floor4 WayPoints.Start"))
        {
            ShowQuizUI();
        }

        // 4층 종료지점일때 퀴즈 끄기
        if (other.gameObject.name.Equals("Floor4 WayPoints.End") && quizUI.gameObject.activeSelf)
        {
            OnSelectRightAnswer();
        }
    }

    private int CalculateQuizScore() => _quizResult ? 25 : 15;
}
