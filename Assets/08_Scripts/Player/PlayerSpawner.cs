using Photon.Pun;
using UnityEngine;

/* 플레이어 생성 가능한것 추가 */
public enum PlayerEnum {
    Bico,
    Jennie,
    Shak,
    Toto
}

public class PlayerSpawner : MonoBehaviour
{
    public PlayerCharacterSo[] playerCharacterArray;

    private void Awake()
    {
        playerCharacterArray = Resources.LoadAll<PlayerCharacterSo>("Player");
    }

    public GameObject NetworkInstantiate(PlayerEnum playerEnum)
    {
        return NetworkInstantiate(playerEnum, Vector3.zero, Quaternion.identity);
    }

    public GameObject NetworkInstantiate(PlayerEnum playerEnum, Vector3 pos, Quaternion quaternion)
    {
        PlayerCharacterSo selectedChar = playerCharacterArray[(int)playerEnum];
        return PhotonNetwork.Instantiate(selectedChar.characterName, pos, quaternion);
    }
}
