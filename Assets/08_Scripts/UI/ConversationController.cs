using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConversationController : MonoBehaviour
{
    [SerializeField] Image speakerImg;
    [SerializeField] Image speakerName;
    [SerializeField] Image conversationContent;
    [SerializeField] TextMeshProUGUI speakerNameTxt;
    [Header("Speaker 이미지")]
    [SerializeField] Sprite sobaekImage;
    [SerializeField] Sprite dataewooriImage;

    public TextMeshProUGUI conversationTxt { get; private set; }
    private void Start()
    {
        conversationTxt = transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>();
    }

    // 2. 대화창
    // 튜토리얼일때 나레이션으로 출력
    public void PrintNarration()
    {
        speakerImg.enabled = false;
        speakerName.gameObject.SetActive(false);
    }
    // 예방 전/화재 전에 대화창 출력
    public void PrintConversation()
    {
        speakerImg.enabled = true;
        speakerName.gameObject.SetActive(true);
    }

    // 내용 바꾸기
    public void ChangeConversation(string text)
    {
        conversationTxt.text = text;
    }

    public void ChangeDataeWooriImage(UIType type)
    {
        // 받은 UIType에 따라 대화창 이름/이미지 변경
        switch (type)
        {
            case UIType.Sobaek:
                speakerImg.sprite = sobaekImage;
                speakerNameTxt.text = "소백이";
                break;
            case UIType.Dataewoori:
                speakerImg.sprite = dataewooriImage;
                speakerNameTxt.text = "다태우리";
                break;
        }
    }
}
