using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterChoose : MonoBehaviour
{
    [SerializeField] FirebaseManager firebaseMgr;
    [SerializeField] List<ButtonInteractor> checkButtonList;
    [SerializeField] GameObject xrRigObject;

    [System.Serializable]
    class ButtonInteractor
    {
        public PlayerCharacterSo charInfo;
        public Button applyButton;
        public GameObject characterObject;
    }

    private void Start()
    {
        foreach(var button in checkButtonList)
        {
            button.applyButton.onClick.AddListener(() =>
            {
                xrRigObject.GetComponent<CustomTunnelingVignette>().FadeOut();
                SceneController.Instance.SetChooseCharacterType(button.charInfo);
                Debug.Log("캐릭터 이름 : " + SceneController.Instance.GetChooseCharacterType().characterName);
                firebaseMgr.SaveCharacterSelection(SceneController.Instance.GetChooseCharacterType().characterName);
                MoveScene(SceneController.Instance.chooseSceneType);
            });
            button.applyButton.gameObject.SetActive(false);
        }
        // 각자의 checkButtonList 버튼의 작동 로직 필요 => 이전의 씬에 씬 선택을 고른거에 따라 작동이 달라져야 함
        // 옵저버 패턴으로 만약에 버튼이 켜지고 확인 버튼을 누르면 이전의 씬 선택에 따라 다른 씬으로 이동
    }

    public void BackToBeforeScene()
    {
        SceneController.Instance.MoveToSceneChoose();
    }

    public void SetActiveButton(PlayerEnum type)
    {
        foreach (var button in checkButtonList)
        {
            button.applyButton.gameObject.SetActive(button.charInfo.characterType == type);
        }
    }

    public GameObject[] GetCharacterObject()
    {
        GameObject[] obj = new GameObject[checkButtonList.Count];

        for(int i=0; i<checkButtonList.Count; i++)
        {
            obj[i] = checkButtonList[i].characterObject;
        }
        return obj;
    }

    void MoveScene(SceneType type)
    {
        switch (type)
        {
            case SceneType.IngameScene_Fire:
                Debug.Log("IngameScene_Fire");
                SceneController.Instance.MoveToPreventionFireScene();
                break;
            case SceneType.IngameScene_Evacuation:
                Debug.Log("IngameScene_Evacuation");
                SceneController.Instance.MoveToEvacuationScene();
                break;
        }
    }
}
