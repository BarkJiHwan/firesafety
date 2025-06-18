using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(DialogueLoader))]
public class DialoguePlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public DialogueLoader dialogueLoader;

    public event Action onPlayDialougue;
    public event Action onStopDialogue;

    public string PlayWithText(string dialogueId)
    {
        PlayAudio(dialogueId);
        onPlayDialougue?.Invoke();
        StartCoroutine(WaitUntilAudioSourceEnd());
        return dialogueLoader.GetDialogueText(dialogueId);
    }

    public void Stop()
    {
        StopAudio();
        onStopDialogue?.Invoke();
    }

    /* 사운드 재생 */
    private void PlayAudio(string dialogueId)
    {
        AudioClip clip = dialogueLoader.GetAudioClip(dialogueId);
        audioSource.clip = clip;
        audioSource.Play();
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
