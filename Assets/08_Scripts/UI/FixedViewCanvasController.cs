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
    Dataewoori,
    None
}

public class FixedViewCanvasController : MonoBehaviour
{
    [SerializeField] ScoreManager scoreMgr;
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

    [Header("경고창")]
    [SerializeField] GameObject warningPanel;

    [Header("타이머")]
    [SerializeField] GameObject timePanel;

    UIType pastDiaType = UIType.None;

    ScoreBoardController scoreBoardCtrl;
    ConversationController conversationCtrl;

    PlayerTutorial tutorialMgr;

    int scoreStartIndex;

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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameEnd += TurnOnScoreBoard;
        }

        // 2. 대화창
        // 튜토리얼 설정
        if (conversationBoard.activeSelf == true)
        {
            conversationBoard.SetActive(false);
        }

        // 4. 타이머
        if(timePanel.activeSelf == true)
        {
            timePanel.SetActive(false);
        }
        // GameManger OnPhaseChanged 구독
        GameManager.Instance.OnPhaseChanged += TurnTimeBoard;
    }

    // ScoreBoard 켜는 것
    public void TurnOnScoreBoard()
    {
        // 씬 타입에 따라 점수 시작 인덱스 설정
        InitScoreIndex(SceneController.Instance.chooseSceneType);
        // 점수판 UI 활성화
        scorePanel.SetActive(true);
        // 점수 로딩 대기 시작
        StartCoroutine(UpdateBoard());
    }

    // 점수가 모두 로딩될 때까지 대기 후 점수판 업데이트
    IEnumerator UpdateBoard()
    {
        yield return new WaitUntil(() =>
        {
            foreach (int score in scoreMgr.GetScores(scoreStartIndex))
            {
                if (score == 0)
                {
                    return false; // 점수가 아직 0이면 대기
                }
            }
            return true; // 모든 점수가 0이 아니면 진행
        });
        //여기서 한번 기다려야함
        if (scorePanel.activeSelf == true)
        {
            SceneType sceneType = SceneController.Instance.chooseSceneType;
            // 씬 타입에 따라 점수판 기준 변경
            scoreBoardCtrl?.ChangeBoardStandard(sceneType);
            // 일정 시간 후 점수판 닫기
            StartCoroutine(CloseScoreBoard());
        }
    }

    // 점수판 일정 시간 후 자동으로 닫고 다음 씬으로 이동
    IEnumerator CloseScoreBoard()
    {
        float elapsedTime = 0;
        int restSecond = 0;
        while (elapsedTime <= showScoreTime)
        {
            elapsedTime += Time.deltaTime;
            restSecond = Mathf.CeilToInt(showScoreTime - elapsedTime);
            // 남은 시간 갱신
            restSecondText.text = restSecond.ToString();
            yield return null;
        }
        // 점수판 UI 비활성화
        scorePanel.SetActive(false);
        // 점수판 초기화
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

    // 점수 인덱스는 각 씬 타입별로 다르게 지정
    void InitScoreIndex(SceneType type)
    {
        switch (type)
        {
            case SceneType.IngameScene_Fire:
                scoreStartIndex = 0;
                break;
            case SceneType.IngameScene_Evacuation:
                scoreStartIndex = 4;
                break;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameEnd -= TurnOnScoreBoard;
        }
    }


    // 대화창 타입에 따라 위치 등 변경 -> 대화창 켜는 것까지 작동
    public void SwitchConverstaionPanel(UIType type)
    {
        Vector3 pos = narrationPos;
        switch (type)
        {
            case UIType.Narration:
                // 나레이션 위치로 이동
                pos = narrationPos;
                // 나레이션 출력
                conversationCtrl.PrintNarration();
                break;
            case UIType.Sobaek:
            case UIType.Dataewoori:
                // 대화창 위치로 이동
                pos = conversationPos;
                // 대화 출력
                conversationCtrl.PrintConversation();
                // UIType에 따라 캐릭터 이미지 변경
                conversationCtrl.ChangeDataeWooriImage(type);
                break;
        }
        // 대화창 위치 및 활성화
        conversationBoard.GetComponent<RectTransform>().anchoredPosition = pos;
        conversationPanel.SetActive(true);
    }

    // 경고창 활성 상태 반환
    public bool IsWarningSignActive()
    {
        return warningPanel.activeSelf;
    }

    // 경고창 표시 / 숨김
    public void TurnWarningSign(bool isActive)
    {
        warningPanel.SetActive(isActive);
    }

    // 플레이어 인덱스에 따라 배경 색상 변경
    public void ChangeScoreBoardPlayerColor(int index)
    {
        scoreBoardCtrl.SetPlayerImageBack(index);
    }

    // 게임 페이즈에 따라 시간 패널 표시 / 숨김
    public void TurnTimeBoard(GamePhase phase)
    {
        if(phase == GamePhase.Prevention)
        {
            // 예방 단계에서 시간 표시
            timePanel.SetActive(true);
        }
        else if(phase == GamePhase.LeaveDangerArea)
        {
            // LeaveDangerArea 페이즈에서 시간 숨김
            timePanel.SetActive(false);
            // 페이지 변경할 때마다 이벤트 해제
            GameManager.Instance.OnPhaseChanged -= TurnTimeBoard;
        }
    }
}
