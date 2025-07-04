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
        // 해당 점수 아이콘 표시할 위치
        public Image scoreItemImagePos;
        // 점수 아이콘 배열
        public Sprite[] scoreItemImage;
        // 점수 타입
        public ScoreType[] scoreType;
        // 점수 항목 텍스트
        public TextMeshProUGUI scoreItem;
        // 도장 이미지
        public Image stampImage;
    }
    // 점수 항목별 텍스트
    [TextArea] public string[] scoreItemsText;
    // 도장 개수에 따른 멘트 텍스트 위치
    [SerializeField] TextMeshProUGUI mentionPos;
    // 도장 이미지 종류
    [SerializeField] Sprite[] stampTypes;
    // 멘트 문구
    [SerializeField] string[] mentionText;

    // 점수 항목 시작 인덱스
    int startIndex = 0;
    ScoreManager scoreMgr;

    // 점수판 열림 이벤트
    public event Action<SceneType> OnScoreBoardOpen;

    private void Awake()
    {
        scoreMgr = FindObjectOfType<ScoreManager>();
    }

    void Start()
    {
        // 점수판 초기화
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

    // 점수판의 기준을 씬 타입에 따라 변경
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
                // 화재 씬 기준으로 점수판 변경
                ChangeBoard(typeNum, 0);
                break;
            case SceneType.IngameScene_Evacuation:
                typeNum = 1;
                // 탈출 씬 기준으로 점수판 기준 변경
                ChangeBoard(typeNum, scoreItems.Length);
                break;
        }
    }

    // 점수판 아이템 변경 및 도장 표시
    void ChangeBoard(int typeNumber, int startIndex)
    {
        int stampNum = 0;
        // 평가 항목에 따라 아이콘, 글 수정
        for(int i= startIndex; i<scoreItems.Length + startIndex; i++)
        {
            // 해당 타입 점수 아이콘 설정
            scoreItems[i - startIndex].scoreItemImagePos.sprite = scoreItems[i - startIndex].scoreItemImage[typeNumber];
            // 점수 항목 텍스트 설정
            scoreItems[i - startIndex].scoreItem.text = scoreItemsText[i];

            // 평가 점수에 따른 도장 찍기
            // 해당 scoreType 가진 항목의 이미지 SetActive(false);
            ScoreType scoreType = scoreItems[i - startIndex].scoreType[typeNumber];
            bool isCorrect = scoreMgr.IsScorePerfect(scoreType);
            if (isCorrect == true)
            {
                // 도장 이미지 설정
                scoreItems[i - startIndex].stampImage.sprite = GetImageTypeByScore(scoreMgr.GetScore(scoreType));
                //scoreItems[i - startIndex].stampImage.gameObject.SetActive(true);
                scoreItems[i - startIndex].stampImage.enabled = true;
                // 점수가 20점 이상이면 도장 개수 증가
                if (scoreMgr.GetScore(scoreType) >= 20)
                {
                    stampNum++;
                }
            }
        }
        // 도장의 개수에 따라 멘트 달라지기
        mentionPos.text = mentionText[scoreItems.Length - stampNum];
    }

    // 점수에 따라 도장 이미지 타입 반환
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

    // 들어온 순서에 따른 아웃라인 이미지 색상 변경
    public void SetPlayerImageBack(int order)
    {
        outlineImage.color = outlineColor[order];
    }
}
