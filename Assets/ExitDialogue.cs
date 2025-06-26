using UnityEngine;

public class ExitDialogue : MonoBehaviour
{
    public GameObject quizUI;
    private DialoguePlayer _dialoguePlayer;

    private void Start()
    {
        _dialoguePlayer = FindObjectOfType<DialoguePlayer>();
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
    }

    public void OnSelectWrongAnswer()
    {
        HideQuizUI();
        _dialoguePlayer.PlayWithText("EXIT_004", UIType.Sobaek);
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
        if (other.gameObject.name.Equals("Floor4 WayPoints.End"))
        {
            OnSelectRightAnswer();
        }
    }
}
