using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FixedViewCanvasController : MonoBehaviour
{
    [Header("점수판")]
    [SerializeField] GameObject scorePanel;
    [SerializeField] float showScoreTime;
    [SerializeField] TextMeshProUGUI restSecondText; 

    void Start()
    {
        // 임시
        scorePanel.SetActive(true);
        StartCoroutine(CloseScoreBoard());

        // => 옵저버 패턴

        // 1. 점수판
        // 화재 페이즈가 끝나면 점수판 출력 (GameManager.Instance.CurrentPhase == leaveDangerArea) -> 예외처리 필요 : scorePanel이 켜져있을때
    }

    void Update()
    {
        
    }

    IEnumerator CloseScoreBoard()
    {
        float startTime = Time.realtimeSinceStartup;
        float elapsedTime = showScoreTime - startTime;
        int restSecond = Mathf.FloorToInt(elapsedTime);
        restSecondText.text = restSecond.ToString();
        yield return new WaitForSeconds(showScoreTime);
        Debug.Log("끝남");

        // 예방/화재에서는 초가 끝나면 방을 나가서 씬 선택 창으로 이동
        //if(SceneController.Instance.chooseSceneType == SceneType.IngameScene_Fire)
        //{
        //    // 현재 접속되어 있는 방 탈출

        //    // 씬 선택창으로 이동
        //    SceneController.Instance.MoveToSceneChoose();
        //}
        //// 대피에서는 초가 끝나면
        //if(SceneController.Instance.chooseSceneType == SceneType.IngameScene_Evacuation)
        //{
        //    // MainScene으로 이동
        //    SceneController.Instance.MoveToMainScene();
        //}
    }
}
