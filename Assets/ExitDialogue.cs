using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitDialogue : MonoBehaviour
{
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
}
