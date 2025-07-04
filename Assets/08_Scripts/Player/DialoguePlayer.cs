using System;
using System.Collections;
using UnityEngine;

/*
 * DialoguePlayer와 쌍을 이뤄 대화를 재생하는 클래스입니다.
 * 한개 대사를 호출할때는 PlayWithText,
 * 여러개가 쭉 이어지는 대사라면 PlayWithTexts를 사용하도록 구성했습니다.
 * 대화가 끝난 후에 어떤 이벤트를 발생시키고 싶다면
 * onFinishDialogue를 이용해 등록하고, 바로 해재해줍니다
 * (이벤트를 하나만 만들었습니다, 여러개 흐름을 만들어야 한다면 더 추가해야할듯)
 */
[RequireComponent(typeof(DialogueLoader))]
public class DialoguePlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public DialogueLoader dialogueLoader;

    [SerializeField]
    private FixedViewCanvasController _fvCanvasController;
    private bool _isDialoguePlaying;

    public event Action onFinishDialogue;

    public void PlayWithText(string dialogueId, UIType type)
    {
        StartCoroutine(PlayUntilAudioSourceEnd(dialogueId));

        // 텍스트 바꾸고 대화창 켜주기
        string text = dialogueLoader.GetDialogueText(dialogueId);
        _fvCanvasController.ConversationTxt.text = text;
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
        // 다른것 실행중일경우 대기
        yield return new WaitWhile(() => _isDialoguePlaying);
        _isDialoguePlaying = true;

        // 재생
        PlayAudio(dialogueId);

        // 오디오소스 끝날때까지 대기
        yield return new WaitWhile(() => audioSource.isPlaying);

        // 정지
        Stop();
        onFinishDialogue?.Invoke();

        _isDialoguePlaying = false;
    }

    /* 여러개 대화 재생, 오디오 끝날때까지 대기 후 0.3초 더 대기 */
    private IEnumerator PlayTextsUntilAudioSourceEnd(string[] dialogueIds, UIType type)
    {
        // 다른것 실행중일경우 대기
        yield return new WaitWhile(() => _isDialoguePlaying);
        _isDialoguePlaying = true;

        // 재생
        foreach (string dialogueId in dialogueIds)
        {
            PlayAudio(dialogueId);

            // 텍스트 바꾸고 대화창 켜주기
            string text = dialogueLoader.GetDialogueText(dialogueId);
            _fvCanvasController.ConversationTxt.text = text;
            _fvCanvasController.SwitchConverstaionPanel(type);

            // 오디오소스 끝날때까지 대기
            yield return new WaitWhile(() => audioSource.isPlaying);
            Stop();
            yield return new WaitForSeconds(0.3f);
        }

        onFinishDialogue?.Invoke();
        _isDialoguePlaying = false;
    }
}
