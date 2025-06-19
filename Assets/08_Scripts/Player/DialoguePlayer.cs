using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(DialogueLoader))]
public class DialoguePlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public DialogueLoader dialogueLoader;

    [SerializeField]
    private FixedViewCanvasController _fvCanvasController;

    public event Action onFinishDialogue;

    public void PlayWithText(string dialogueId, UIType type)
    {
        StartCoroutine(PlayUntilAudioSourceEnd(dialogueId));

        // 텍스트 바꾸고 대화창 켜주기
        string text = dialogueLoader.GetDialogueText(dialogueId);
        _fvCanvasController.ConversationTxt.text = text;
        //_fvCanvasController.ConversationPanel.SetActive(true);
        _fvCanvasController.SwitchConverstaionPanel(type);
    }

    public void PlayWithTexts(string[] dialogueIds, UIType type)
    {
        StartCoroutine(PlayTextsUntilAudioSourceEnd(dialogueIds, type));
    }

    // 오디오 끄고 대화창 꺼주기
    public void Stop()
    {
        if (audioSource.isPlaying)
        {
            StopAudio();
        }

        _fvCanvasController.ConversationPanel.SetActive(false);
    }

    /* 사운드 재생 */
    private void PlayAudio(string dialogueId)
    {
        AudioClip clip = dialogueLoader.GetAudioClip(dialogueId);

        if (clip != null)
        {
            audioSource.clip = clip;
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

    /* 종료까지 기다림 */
    private IEnumerator PlayUntilAudioSourceEnd(string dialogueId)
    {
        PlayAudio(dialogueId);
        yield return new WaitWhile(() => audioSource.isPlaying);
        Stop();
        onFinishDialogue?.Invoke();
    }

    /* 여러개 대화 재생, 오디오 끝날때까지 대기 후 0.3초 더 대기 */
    private IEnumerator PlayTextsUntilAudioSourceEnd(string[] dialogueIds, UIType type)
    {
        foreach (string dialogueId in dialogueIds)
        {
            PlayAudio(dialogueId);
            // 텍스트 바꾸고 대화창 켜주기
            string text = dialogueLoader.GetDialogueText(dialogueId);
            _fvCanvasController.ConversationTxt.text = text;
            //_fvCanvasController.ConversationPanel.SetActive(true);
            _fvCanvasController.SwitchConverstaionPanel(type);

            yield return new WaitWhile(() => audioSource.isPlaying);
            Stop();
            yield return new WaitForSeconds(0.3f);
        }

        onFinishDialogue?.Invoke();
    }
}
