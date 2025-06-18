using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(DialogueLoader))]
public class DialoguePlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public DialogueLoader dialogueLoader;
    public FixedViewCanvasController fvCanvasController;

    private AudioClip _currentClip;
    public void PlayWithText(string dialogueId)
    {
        PlayAudio(dialogueId);
        StartCoroutine(WaitUntilAudioSourceEnd());

        string text = dialogueLoader.GetDialogueText(dialogueId);
        // 대화창 켜주기
        fvCanvasController.ConversationTxt.text = text;
        fvCanvasController.ConversationPanel.SetActive(true);
    }

    public void PlayWithTexts(string[] dialogueId)
    {

    }


    public void Stop()
    {
        StopAudio();

        // 오디오 끄고 대화창 꺼주기
        fvCanvasController.ConversationPanel.SetActive(false);
    }

    /* 사운드 재생 */
    private void PlayAudio(string dialogueId)
    {
        _currentClip = dialogueLoader.GetAudioClip(dialogueId);

        if (_currentClip != null)
        {
            audioSource.clip = _currentClip;
            audioSource.Play();
        }
    }

    /* 사운드 재생 중지 */
    private void StopAudio()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private IEnumerator WaitUntilAudioSourceEnd()
    {
        yield return new WaitWhile(() => audioSource.isPlaying);
        Stop();
    }
}
