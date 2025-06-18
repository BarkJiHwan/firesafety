using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(DialogueLoader))]
public class DialoguePlayer : MonoBehaviour
{
    private AudioListener _audioListener;

    public DialogueLoader dialogueLoader;

    public UnityAction OnPlayDialougue;
    public UnityAction OnStopDialogue;

    public AudioListener AudioListener
    {
        get => _audioListener;
        set => _audioListener = value;
    }

    public void Play()
    {
        PlayAudio();
        OnPlayDialougue.Invoke();
    }

    public void Stop()
    {
        StopAudio();
        OnStopDialogue.Invoke();
    }

    /* 사운드 재생 */
    private void PlayAudio()
    {

    }

    /* 사운드 재생 중지 */
    private void StopAudio()
    {

    }

}
