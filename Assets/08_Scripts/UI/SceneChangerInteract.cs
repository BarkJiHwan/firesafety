using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class SceneChangerInteract : MonoBehaviour
{
    [SerializeField] Button firePhase;
    [SerializeField] Button evacuationPhase;
    void Start()
    {
        // XR Origin으로 예방/화재 버튼을 누르면 캐릭터 선택창으로 이동
        firePhase.onClick.AddListener(() =>
        {
            // 고른 Scene 타입 저장
            SceneController.Instance.chooseSceneType = SceneType.IngameScene_Fire;
            Debug.Log("SceneType : " + SceneController.Instance.chooseSceneType);
            SceneController.Instance.MoveToCharacterScene();
        });
        // XR Origin으로 탈출 버튼을 누르면 캐릭터 선택창으로 이동
        evacuationPhase.onClick.AddListener(() =>
        {
            SceneController.Instance.chooseSceneType = SceneType.IngameScene_Evacuation;
            Debug.Log("SceneType : " + SceneController.Instance.chooseSceneType);
            SceneController.Instance.MoveToCharacterScene();
        });
    }
}
