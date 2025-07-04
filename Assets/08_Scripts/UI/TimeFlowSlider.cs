using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeFlowSlider : MonoBehaviour
{
    [SerializeField] Slider timeSlider;
    [SerializeField] Image characterImg;

    void Start()
    {
        if (SceneController.Instance != null)
        {
            characterImg.sprite = SceneController.Instance.GetChooseCharacterType().characterImage;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if(GameManager.Instance.CurrentPhase >= GamePhase.Prevention)
        {
            if(GameManager.Instance.IsPausing == true)
            {
                return;
            }
            else
            {
                // 현재 시간 받아오기
                //GameManager.Instance.GameTimer;
                DivideTimer();
            }
        }
    }

    void DivideTimer()
    {
        // 현재 게임 경과 시간 가져오기
        float currentTime = GameManager.Instance.GameTimer;

        // 각 게임 페이즈의 시작 시간 배열 넣기
        float[] time = new float[(int)GamePhase.LeaveDangerArea + 1];
        time = GameManager.Instance.StartTime;

        // 슬라이더에 적용할 값 초기화
        float sliderValue = 0;
        // 현재 시간이 Prevention ~ Fire 사이일 경우 (예방 구간)
        if(currentTime >= time[(int)GamePhase.Prevention] && currentTime <= time[(int)GamePhase.Fire])
        {
            // 구간 내 진행률 계산 (0 ~ 1 사이)
            float t = (currentTime - 1f) / (time[(int)GamePhase.Fire] - time[(int)GamePhase.Prevention]);
            // 슬라이더 값 0 ~ 0.33 사이로 보간
            sliderValue = Mathf.Lerp(0f, 0.33f, t);
        }
        // 현재 시간이 Fire ~ Fever 사이일 경우 (화재 구간)
        if (currentTime > time[(int)GamePhase.Fire] && currentTime <= time[(int)GamePhase.Fever])
        {
            // 구간 내 진행률 계산
            float t = (currentTime - time[(int)GamePhase.Fire]) / (time[(int)GamePhase.Fever] - time[(int)GamePhase.Fire]);
            // 슬라이더 값 0.33 ~ 0.66 사이로 보간
            sliderValue = Mathf.Lerp(0.33f, 0.66f, t);
        }
        // 현재 시간이 Fever ~ LeaveDangerArea 사이일 경우 (피버 구간)
        if (currentTime > time[(int)GamePhase.Fever] && currentTime <= time[(int)GamePhase.LeaveDangerArea])
        {
            // 구간 내 진행률 계산
            float t = (currentTime - time[(int)GamePhase.Fever]) / (time[(int)GamePhase.LeaveDangerArea] - time[(int)GamePhase.Fever]);
            // 슬라이더 값 0.66 ~ 1.0 사이로 보간
            sliderValue = Mathf.Lerp(0.66f, 1f, t);
        }

        // 계산된 슬라이더 값을 UI 슬라이더에 적용
        timeSlider.value = sliderValue;
    }
}
