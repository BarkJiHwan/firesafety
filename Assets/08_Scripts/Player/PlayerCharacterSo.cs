using UnityEngine;

/*
 * 플레이어 캐릭터의 정보들 (프리팹, 명칭, 이미지등) 을 다룰수 있는 Scriptable Object 입니다.
 * 플레이어 정보가 필요한 게임의 요소에서 사용됩니다.
 */
[CreateAssetMenu(fileName = "CharacterData", menuName= "Scriptable Object/Character Data")]
public class PlayerCharacterSo : ScriptableObject
{
    [Header("캐릭터 프리팹")]
    public GameObject characterPrefab;
    public GameObject characterPrefabSingle;

    [Header("캐릭터 영어 / 한글 이름")]
    public string characterName;
    public string characterKrName;

    public Sprite characterImage;
    public PlayerEnum characterType;


}
