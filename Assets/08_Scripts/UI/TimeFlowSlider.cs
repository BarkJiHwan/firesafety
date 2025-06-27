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
        characterImg.sprite = SceneController.Instance.GetChooseCharacterType().characterImage;
    }

    // Update is called once per frame
    void Update()
    {
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
        float currentTime = GameManager.Instance.GameTimer;
        float[] time = new float[(int)GamePhase.LeaveDangerArea + 1];
        time = GameManager.Instance.StartTime;
        float sliderValue = 0;
        if(currentTime >= time[(int)GamePhase.Prevention] && currentTime <= time[(int)GamePhase.Fire])
        {
            float t = (currentTime - 1f) / (time[(int)GamePhase.Fire] - time[(int)GamePhase.Prevention]);
            sliderValue = Mathf.Lerp(0f, 0.33f, t);
        }
        if (currentTime > time[(int)GamePhase.Fire] && currentTime <= time[(int)GamePhase.Fever])
        {
            float t = (currentTime - time[(int)GamePhase.Fire]) / (time[(int)GamePhase.Fever] - time[(int)GamePhase.Fire]);
            sliderValue = Mathf.Lerp(0.33f, 0.66f, t);
        }
        if (currentTime > time[(int)GamePhase.Fever] && currentTime <= time[(int)GamePhase.LeaveDangerArea])
        {
            float t = (currentTime - time[(int)GamePhase.Fever]) / (time[(int)GamePhase.LeaveDangerArea] - time[(int)GamePhase.Fever]);
            sliderValue = Mathf.Lerp(0.66f, 1f, t);
        }
        timeSlider.value = sliderValue;
    }
}
