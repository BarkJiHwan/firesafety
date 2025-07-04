using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] CharacterChoose charChoose;

    public GameObject selectCharacter { get; set; }

    // 선택한 캐릭터와 그 외 캐릭터 처리
    public void MakeChooseCharacter()
    {
        for (int i = 0; i < charChoose.GetCharacterObject().Length; i++)
        {
            // 선택한 캐릭터는 기존의 원래 Material로 변경
            if (charChoose.GetCharacterObject()[i] == selectCharacter)
            {
                charChoose.GetCharacterObject()[i].GetComponent<ChooseCharacterManager>().ChangeOriginMaterial();
            }
            // 그 외의 캐릭터는 회색으로 변경
            else
            {
                charChoose.GetCharacterObject()[i].GetComponent<ChooseCharacterManager>().ChangeMaterialToGrey();
            }
        }
    }
}
