using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] CharacterChoose charChoose;

    public GameObject selectCharacter { get; set; }

    private void Start()
    {
        
    }

    public void MakeChooseCharacter()
    {
        for (int i = 0; i < charChoose.GetCharacterObject().Length; i++)
        {
            if (charChoose.GetCharacterObject()[i] == selectCharacter)
            {
                charChoose.GetCharacterObject()[i].GetComponent<ChooseCharacterManager>().ChangeOriginMaterial();
            }
            else
            {
                Debug.Log(charChoose.GetCharacterObject()[i]);
                charChoose.GetCharacterObject()[i].GetComponent<ChooseCharacterManager>().ChangeMaterialToGrey();
            }
        }
    }

    // 임시
    public AsyncOperation LoadScene(int sceneNum)
    {
        return SceneManager.LoadSceneAsync(sceneNum);
    }

    public void ChangePracticeScene()
    {
        LoadScene((int)3);
    }

}
