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

    void Start()
    {
        // 임시
        //SceneController.Instance.chooseSceneType = SceneType.IngameScene_Fire;
        //ChangeBoardStandard(SceneController.Instance.chooseSceneType);

        scoreMgr = FindObjectOfType<ScoreManager>();
        InitateScoreBoard();

        // 테스트
        //SceneType type = SceneType.IngameScene_Evacuation;
        //scoreMgr.SetScore(ScoreType.Prevention_Count, 19);
        //scoreMgr.SetScore(ScoreType.Prevention_Time, 25);
        //scoreMgr.SetScore(ScoreType.Fire_Count, 20);
        //scoreMgr.SetScore(ScoreType.Fire_Time, 5);
        //scoreMgr.SetScore(ScoreType.Elevator, 15);
        //scoreMgr.SetScore(ScoreType.Smoke, 10);
        //scoreMgr.SetScore(ScoreType.Taewoori_Count, 25);
        //scoreMgr.SetScore(ScoreType.DaTaewoori, 17);
        //ChangeBoardStandard(type);
    }

    void Update()
    {
        
    }

    // 점수판 초기화
    public void InitateScoreBoard()
    {
        for(int i=0; i<scoreItems.Length; i++)
        {
            scoreItems[i].stampImage.gameObject.SetActive(false);
        }
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
            ScoreType type = scoreItems[i - startIndex].scoreType[typeNumber];
            bool isCorrect = scoreMgr.IsScorePerfect(type);
            if (isCorrect == true)
            {
                scoreItems[i - startIndex].stampImage.sprite = GetImageTypeByScore(scoreMgr.GetScore(type));
                scoreItems[i - startIndex].stampImage.gameObject.SetActive(true);
                if(scoreMgr.GetScore(type) >= 20)
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
            case >= 20:
                return stampTypes[0];
            case >= 15:
                return stampTypes[1];
        }
        return null;
    }

    private void OnEnable()
    {
        ChangeBoardStandard(SceneController.Instance.chooseSceneType);
    }
}
