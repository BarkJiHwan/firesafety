using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBoardController : MonoBehaviour
{
    [Header("캐릭터 이미지")]
    [SerializeField] Image outlineImage;
    [SerializeField] Image charcaterImage;
    [Header("평가 항목")]
    [SerializeField] TextMeshProUGUI[] scoreItems;
    [TextArea] public string[] scoreItemsText;

    int startIndex = 0;
    void Start()
    {
        // 임시
        SceneController.Instance.chooseSceneType = SceneType.IngameScene_Evacuation;

        if(SceneController.Instance.chooseSceneType == SceneType.IngameScene_Fire)
        {
            startIndex = 0;
        }
        else if (SceneController.Instance.chooseSceneType == SceneType.IngameScene_Evacuation)
        {
            startIndex = 4;
        }
        for (int i = startIndex; i < scoreItems.Length + startIndex; i++)
        {
            scoreItems[i - startIndex].text = scoreItemsText[i];
        }
    }

    void Update()
    {
        
    }
}
