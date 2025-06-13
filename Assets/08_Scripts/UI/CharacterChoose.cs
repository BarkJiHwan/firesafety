using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterChoose : MonoBehaviour
{
    [SerializeField] List<ButtonInteractor> checkButtonList;
    [System.Serializable]
    class ButtonInteractor
    {
        public PlayerEnum charType;
        public Button applyButton;
        public GameObject characterObject;
    }

    private void Start()
    {
        foreach(var button in checkButtonList)
        {
            button.applyButton.gameObject.SetActive(false);
        }
    }

    public void BackToBeforeScene()
    {
        SceneController.Instance.MoveToSceneChoose();
    }

    public void SetActiveButton(PlayerEnum type)
    {
        foreach (var button in checkButtonList)
        {
            button.applyButton.gameObject.SetActive(button.charType == type);
        }
    }

    public GameObject[] GetCharacterObject()
    {
        GameObject[] obj = new GameObject[checkButtonList.Count];
        int count = 0;
        for(int i=0; i<checkButtonList.Count; i++)
        {
            obj[i] = checkButtonList[i].characterObject;
        }
        return obj;
    }
}
