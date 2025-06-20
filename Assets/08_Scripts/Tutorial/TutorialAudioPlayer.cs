using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialAudioPlayer : MonoBehaviour
{
    public AudioSource _tutoAudio;
    public DialogueLoader _dialogueLoader;
    [SerializeField] private FixedViewCanvasController _fvCanvasController;

    public bool _istutoAudioPlay = false;

    /* 사운드 및 텍스트 재생 */
    public void PlayVoiceWithText(string dialogueId, UIType type)
    {
        AudioClip clip = _dialogueLoader.GetAudioClip(dialogueId);

        if (clip != null)
        {
            _tutoAudio.clip = clip;
            _tutoAudio.Play();
        }
        string text = _dialogueLoader.GetDialogueText(dialogueId);
        _fvCanvasController.ConversationTxt.text = text;
        _fvCanvasController.SwitchConverstaionPanel(type);
    }
    /* 사운드 및 텍스트 끄기 */
    public void TutorialAudioWithTextStop()
    {
        if (_tutoAudio.isPlaying)
        {
            StopAudio();
        }

        _fvCanvasController.ConversationPanel.SetActive(false);
    }

    /* 사운드 재생 중지 */
    public void StopAudio()
    {
        if (_tutoAudio.isPlaying)
        {
            _tutoAudio.Stop();
        }
    }
}
