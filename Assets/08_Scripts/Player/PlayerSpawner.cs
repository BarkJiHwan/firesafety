using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

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

    public void NetworkInstantiate(PlayerEnum playerEnum)
    {
        NetworkInstantiate(playerEnum, Vector3.zero, Quaternion.identity);
    }

    public void NetworkInstantiate(PlayerEnum playerEnum, Vector3 pos, Quaternion quaternion)
    {
        PlayerCharacterSo selectedChar = playerCharacterArray[(int)playerEnum];
        PhotonNetwork.Instantiate(selectedChar.characterName, pos, quaternion);
    }
}
