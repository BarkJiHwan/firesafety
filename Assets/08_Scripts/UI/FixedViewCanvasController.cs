using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    ScoreBoardController scoreBoardCtrl;
    ConversationController conversationCtrl;
    

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
        // 확인을 위해 잠시 켜기
        //if(conversationBoard.activeSelf == true)
        //{
        //    conversationBoard.SetActive(false);
        //}
        // 튜토리얼 시작하면 대화창(나레이션) 출력 => narrationPos로 옮기고 PrintNarration() 실행

        // 튜토리얼 끝나면 대화창(나레이션) 끄기 => converstaionBoard.SetActive(false);
        // ▲ TutorialMgr에서 실행
        // ==============================================================================

        // 예방 페이즈 시작하기 전에 대화창(소백이) 출력 => conversationPos로 옮기고 PrintConversation() 실행

        // 예방 페이즈 시작하면 대화창(소백이) 끄기 => converstaionBoard.SetActive(false);

        // 화재 페이즈 시작하기 전에 대화창(소백이) 출력 => conversationPos로 옮기고 PrintConversation() 실행

        // 화재 페이즈 시작하면 대화창(소백이) 끄기 => converstaionBoard.SetActive(false);
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
        Debug.Log("끝남");
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
}
