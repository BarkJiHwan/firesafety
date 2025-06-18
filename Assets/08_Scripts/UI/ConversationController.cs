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

    public TextMeshProUGUI conversationTxt { get; private set; }
    private void Start()
    {
        conversationTxt = transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>();
    }

    // 2. 대화창
    // 튜토리얼일때 나레이션으로 출력
    public void PrintNarration()
    {
        if (speakerImg.enabled == true || speakerName.enabled == true)
        {
            speakerImg.enabled = false;
            speakerName.enabled = false;
        }
    }
    // 예방 전/화재 전에 대화창 출력
    public void PrintConversation()
    {
        if (speakerImg.enabled == false || speakerName.enabled == false)
        {
            speakerImg.enabled = true;
            speakerName.enabled = true;
        }
    }

    // 내용 바꾸기
    public void ChangeConversation(string text)
    {
        conversationTxt.text = text;
    }
}
