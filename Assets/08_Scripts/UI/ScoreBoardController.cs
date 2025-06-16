using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBoardController : MonoBehaviour
{
    [Header("캐릭터 이미지")]
    // 들어온 순서에 따른 색상 변경 => 색상 기억
    [SerializeField] Image outlineImage;
    // 고른 캐릭터에 따라 이미지 변경
    [SerializeField] Image charcaterImage;

    [Header("평가 항목")]
    [SerializeField] ScoreItems[] scoreItems;
    [System.Serializable]
    class ScoreItems
    {
        public Image scoreItemImagePos;
        public Sprite[] scoreItemImage;
        public TextMeshProUGUI scoreItem;
        public Image stampImage;
    }
    [TextArea] public string[] scoreItemsText;
    [SerializeField] TextMeshProUGUI mentionPos;
    [SerializeField] string[] mentionText;

    int startIndex = 0;
    void Start()
    {
        // 임시
        //SceneController.Instance.chooseSceneType = SceneType.IngameScene_Fire;
        //ChangeBoardStandard(SceneController.Instance.chooseSceneType);

        SceneType type = SceneType.IngameScene_Fire;
        ChangeBoardStandard(type);
    }

    void Update()
    {
        
    }

    public void ChangeBoardStandard(SceneType type)
    {
        int typeNum = 0;
        switch (type)
        {
            case SceneType.IngameScene_Fire:
                typeNum = 0;
                ChangeBoard(typeNum, 0);
                break;
            case SceneType.IngameScene_Evacuation:
                typeNum = 1;
                ChangeBoard(typeNum, scoreItems.Length);
                break;
        }
    }

    public void ChangeBoard(int typeNumber, int startIndex)
    {
        // 평가 항목에 따라 아이콘, 글 수정
        for(int i= startIndex; i<scoreItems.Length + startIndex; i++)
        {
            scoreItems[i - startIndex].scoreItemImagePos.sprite = scoreItems[i - startIndex].scoreItemImage[typeNumber];
            scoreItems[i - startIndex].scoreItem.text = scoreItemsText[i];
        }
        // 평가 점수에 따른 도장 찍기

        // 도장의 개수에 따라 멘트 달라지기

    }
}
