using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CharacterData", menuName= "Scriptable Object/Character Data")]
public class PlayerCharacterSo : ScriptableObject
{
    [Header("캐릭터 프리팹")]
    public GameObject characterPrefab;

    [Header("캐릭터 영어 / 한글 이름")]
    public string characterName;
    public string characterKrName;

    public Sprite characterImage;
}
