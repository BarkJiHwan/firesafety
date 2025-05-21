using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectUICtrl : MonoBehaviour
{
    [Header("제일 아래 배경")]
    [SerializeField] Image backImage;
    [SerializeField] Color backColor;

    [Header("대화창 배경")]
    [SerializeField] Image ConverBackImage;
    [SerializeField] Color converBackColor;

    [Header("대화창 대화 Text")]
    [SerializeField] TextMeshProUGUI preventWord;
    [SerializeField] float fontSize;
    [SerializeField] Color fontColor;

    [Header("아이콘")]
    [SerializeField] Image iconImg;
    [SerializeField] Sprite warningIcon;
    [SerializeField] Sprite completeIcon;

    void Start()
    {
        // 초기 세팅
        backImage.color = backColor;
        ConverBackImage.color = converBackColor;
        preventWord.fontSize = fontSize;
        preventWord.color = fontColor;
        iconImg.sprite = warningIcon;
    }

    void Update()
    {
        
    }


}
