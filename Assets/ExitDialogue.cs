using System.Collections;
using System.Collections.Generic;
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
}
