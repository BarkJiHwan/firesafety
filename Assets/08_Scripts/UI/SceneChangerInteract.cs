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
        firePhase.onClick.AddListener(() =>
        {
            SceneController.Instance.chooseSceneType = SceneType.IngameScene_Fire;
            Debug.Log("SceneType : " + SceneController.Instance.chooseSceneType);
            SceneController.Instance.MoveToCharacterScene();
        });
        evacuationPhase.onClick.AddListener(() =>
        {
            SceneController.Instance.chooseSceneType = SceneType.IngameScene_Evacuation;
            Debug.Log("SceneType : " + SceneController.Instance.chooseSceneType);
            SceneController.Instance.MoveToCharacterScene();
        });
    }
}
