using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Start()
    {
        PlayerEnum selectedChar = PlayerEnum.Bico;

        if (SceneController.Instance != null
            && SceneController.Instance.GetChooseCharacterType() != null)
        {
            selectedChar = SceneController.Instance.GetChooseCharacterType().characterType;
        }

        if (SceneManager.GetActiveScene().name.Equals("ExitScenes_CHM.Test") ||
             SceneController.Instance.chooseSceneType == SceneType.IngameScene_Evacuation)
        {
            LocalInstantiate(selectedChar);
        }
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

    public GameObject LocalInstantiate(PlayerEnum playerEnum)
    {
        PlayerCharacterSo selectedChar = playerCharacterArray[(int)playerEnum];
        return Instantiate(selectedChar.characterPrefabSingle, transform.position, transform.rotation);
    }
}
