using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIType
{
    Narration,
    Sobaek,
    None
}

public class FixedViewCanvasController : MonoBehaviour
{
    [Header("점수판")]
    [SerializeField] GameObject scorePanel;
    [SerializeField] GameObject conversationBoard;
    [SerializeField] float showScoreTime;
    [SerializeField] TextMeshProUGUI restSecondText;

    [Header("대화창")]
    [SerializeField] GameObject conversationPanel;
    // 튜토리얼일 경우에 panel을 해당 위치로 변경
    [SerializeField] Vector3 narrationPos;
    // 예방/화재 페이즈 전 소백이 대화 위치
    [SerializeField] Vector3 conversationPos;
    [SerializeField] TextMeshProUGUI conversationTxt;

    UIType pastDiaType = UIType.None;

    ScoreBoardController scoreBoardCtrl;
    ConversationController conversationCtrl;

    public GameObject ConversationPanel => conversationPanel;

    public TextMeshProUGUI ConversationTxt => conversationTxt;

    private void Awake()
    {
        scoreBoardCtrl = scorePanel.GetComponent<ScoreBoardController>();
        conversationCtrl = conversationBoard.GetComponent<ConversationController>();
    }

    void Start()
    {
        // 임시 => 예방/화재 이후, 엔딩씬 이후로 구분 필요
        //TurnOnScoreBoard();
        if(scorePanel.activeSelf == true)
        {
            scorePanel.SetActive(false);
        }
        // => 옵저버 패턴

        // 1. 점수판
        // 화재 페이즈가 끝나면 점수판 출력 (GameManager.Instance.CurrentPhase == leaveDangerArea)
        GameManager.Instance.OnGameEnd += TurnOnScoreBoard;

        // 2. 대화창
        // 튜토리얼 설정
        if (conversationBoard.activeSelf == true)
        {
            conversationBoard.SetActive(false);
        }
        SwitchConverstaionPanel(UIType.Narration);
    }

    void TurnOnScoreBoard()
    {
        scorePanel.SetActive(true);
        StartCoroutine(UpdateBoard());
    }

    IEnumerator UpdateBoard()
    {
        yield return new WaitForEndOfFrame();
        if (scorePanel.activeSelf == true)
        {
            SceneType sceneType = SceneController.Instance.chooseSceneType;
            scoreBoardCtrl?.ChangeBoardStandard(sceneType);
            StartCoroutine(CloseScoreBoard());
        }
    }

    IEnumerator CloseScoreBoard()
    {
        float elapsedTime = 0;
        int restSecond = 0;
        while (elapsedTime <= showScoreTime)
        {
            elapsedTime += Time.deltaTime;
            restSecond = Mathf.CeilToInt(showScoreTime - elapsedTime);
            restSecondText.text = restSecond.ToString();
            yield return null;
        }
        scorePanel.SetActive(false);
        scoreBoardCtrl.InitateScoreBoard();

        // 예방/화재에서는 초가 끝나면 방을 나가서 씬 선택 창으로 이동
        if (SceneController.Instance.chooseSceneType == SceneType.IngameScene_Fire)
        {
            // 현재 접속되어 있는 방 탈출
            PhotonNetwork.LeaveRoom();
            // 씬 선택창으로 이동
            SceneController.Instance.MoveToSceneChoose();
        }
        // 대피에서는 초가 끝나면
        if (SceneController.Instance.chooseSceneType == SceneType.IngameScene_Evacuation)
        {
            // MainScene으로 이동
            SceneController.Instance.MoveToMainScene();
        }
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameEnd -= TurnOnScoreBoard;
    }

    public void SwitchConverstaionPanel(UIType type)
    {
        if(pastDiaType == type)
        {
            return;
        }
        pastDiaType = type;
        Vector3 pos = narrationPos;
        switch (type)
        {
            case UIType.Narration:
                pos = narrationPos;
                conversationCtrl.PrintNarration();
                break;
            case UIType.Sobaek:
                pos = conversationPos;
                conversationCtrl.PrintConversation();
                break;
        }
        conversationBoard.GetComponent<RectTransform>().anchoredPosition = pos;
    }
}
