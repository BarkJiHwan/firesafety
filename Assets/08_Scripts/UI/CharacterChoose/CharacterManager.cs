using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 임시
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] CharacterChoose charChoose;

    public GameObject selectCharacter { get; set; }

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
}
