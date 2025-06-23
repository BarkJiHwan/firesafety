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
    [SerializeField] Color[] outlineColor;
    // 고른 캐릭터에 따라 이미지 변경
    [SerializeField] Image charcaterImage;

    [Header("평가 항목")]
    [SerializeField] ScoreItems[] scoreItems;
    [System.Serializable]
    class ScoreItems
    {
        public Image scoreItemImagePos;
        public Sprite[] scoreItemImage;
        public ScoreType[] scoreType;
        public TextMeshProUGUI scoreItem;
        public Image stampImage;
    }
    [TextArea] public string[] scoreItemsText;
    [SerializeField] TextMeshProUGUI mentionPos;
    [SerializeField] Sprite[] stampTypes;
    [SerializeField] string[] mentionText;

    int startIndex = 0;
    ScoreManager scoreMgr;

    public event Action<SceneType> OnScoreBoardOpen;

    private void Awake()
    {
        scoreMgr = FindObjectOfType<ScoreManager>();
    }

    void Start()
    {
        InitateScoreBoard();
        // 선택한 캐릭터 이미지로 변경
        charcaterImage.sprite = SceneController.Instance.GetChooseCharacterType().characterImage;
    }

    void Update()
    {
        
    }

    // 점수판 초기화
    public void InitateScoreBoard()
    {
        for(int i=0; i<scoreItems.Length; i++)
        {
            //scoreItems[i].stampImage.gameObject.SetActive(false);
            scoreItems[i].stampImage.enabled = false;
        }
    }

    public void ChangeBoardStandard(SceneType type)
    {
        if (scoreMgr == null)
        {
            scoreMgr = FindObjectOfType<ScoreManager>();
        }
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

    void ChangeBoard(int typeNumber, int startIndex)
    {
        int stampNum = 0;
        // 평가 항목에 따라 아이콘, 글 수정
        for(int i= startIndex; i<scoreItems.Length + startIndex; i++)
        {
            scoreItems[i - startIndex].scoreItemImagePos.sprite = scoreItems[i - startIndex].scoreItemImage[typeNumber];
            scoreItems[i - startIndex].scoreItem.text = scoreItemsText[i];

            // 평가 점수에 따른 도장 찍기
            // 해당 scoreType 가진 항목의 이미지 SetActive(false);
            ScoreType scoreType = scoreItems[i - startIndex].scoreType[typeNumber];
            bool isCorrect = scoreMgr.IsScorePerfect(scoreType);
            if (isCorrect == true)
            {
                scoreItems[i - startIndex].stampImage.sprite = GetImageTypeByScore(scoreMgr.GetScore(scoreType));
                //scoreItems[i - startIndex].stampImage.gameObject.SetActive(true);
                scoreItems[i - startIndex].stampImage.enabled = true;
                if (scoreMgr.GetScore(scoreType) >= 20)
                {
                    stampNum++;
                }
            }
        }
        // 도장의 개수에 따라 멘트 달라지기
        mentionPos.text = mentionText[scoreItems.Length - stampNum];
    }

    Sprite GetImageTypeByScore(float score)
    {
        switch (score)
        {
            case >= 25:
                return stampTypes[0];
            case >= 20:
                return stampTypes[1];
        }
        return null;
    }

    public void SetPlayerImageBack(int order)
    {
        outlineImage.color = outlineColor[order];
    }
}
