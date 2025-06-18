using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(DialogueLoader))]
public class DialoguePlayer : MonoBehaviour
{
    public DialogueLoader dialogueLoader;

    public UnityAction OnPlayDialougue;
    public UnityAction OnStopDialogue;

    public AudioSource currentAudioSource;

    public string PlayWithText(string dialogueId)
    {
        PlayAudio(dialogueId);
        OnPlayDialougue.Invoke();
        StartCoroutine(WaitUntilAudioSourceEnd());
        return dialogueLoader.GetDialogueText(dialogueId);
    }

    public void Stop()
    {
        StopAudio();
        OnStopDialogue.Invoke();
    }

    /* 사운드 재생 */
    private void PlayAudio(string dialogueId)
    {
        AudioSource source = dialogueLoader.GetAudioSource(dialogueId);
        source.Play();
    }

    /* 사운드 재생 중지 */
    private void StopAudio()
    {
        if (currentAudioSource.isPlaying)
        {
            currentAudioSource.Stop();
        }
    }

    private IEnumerator WaitUntilAudioSourceEnd()
    {
        yield return new WaitWhile(() => currentAudioSource.isPlaying);
        Stop();
    }
}
