using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

public enum PlayerEnum
{
    Bico,
    Jennie,
    Shak,
    Toto
}

public class PlayerSpawner : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("플레이어 설정")]
    public PlayerCharacterSo[] playerCharacterArray;

    [Header("소백이 & 소백카 설정")]
    [SerializeField] private GameObject sobaekPrefab;
    [SerializeField] private GameObject sobaekCarPrefab;
    [SerializeField] private SplineContainer carTrack;
    #endregion

    #region 정적 참조
    private static GameObject currentSobaekCar;
    #endregion

    #region 유니티 라이프사이클
    private void Awake()
    {
        LoadPlayerResources();
        LoadSobaekResources();
    }

    private void Start()
    {
        SpawnPlayerInTargetScenes();
    }
    #endregion

    #region 초기화
    private void LoadPlayerResources()
    {
        playerCharacterArray = Resources.LoadAll<PlayerCharacterSo>("Player");
    }

    private void LoadSobaekResources()
    {
        if (sobaekPrefab == null)
        {
            sobaekPrefab = Resources.Load<GameObject>("Sobaek");
        }

        if (sobaekCarPrefab == null)
        {
            sobaekCarPrefab = Resources.Load<GameObject>("SobaekCar");
        }

        if (carTrack == null)
        {
            carTrack = FindObjectOfType<SplineContainer>();
        }
    }

    private void SpawnPlayerInTargetScenes()
    {
        if (!IsTargetScene())
            return;

        PlayerEnum selectedChar = GetSelectedCharacter();
        GameObject player = LocalInstantiate(selectedChar);

        if (player != null)
        {
            AttachSobaekToPlayer(player);
        }
    }

    private bool IsTargetScene()
    {
        return SceneManager.GetActiveScene().name.Equals("ExitScenes_CHM.Test") ||
               SceneController.Instance.chooseSceneType == SceneType.IngameScene_Evacuation;
    }

    private PlayerEnum GetSelectedCharacter()
    {
        if (SceneController.Instance?.GetChooseCharacterType() != null)
        {
            return SceneController.Instance.GetChooseCharacterType().characterType;
        }
        return PlayerEnum.Bico;
    }
    #endregion

    #region 플레이어 생성
    public GameObject NetworkInstantiate(PlayerEnum playerEnum)
    {
        return NetworkInstantiate(playerEnum, Vector3.zero, Quaternion.identity);
    }

    public GameObject NetworkInstantiate(PlayerEnum playerEnum, Vector3 pos, Quaternion quaternion)
    {
        PlayerCharacterSo selectedChar = playerCharacterArray[(int)playerEnum];
        GameObject player = PhotonNetwork.Instantiate(selectedChar.characterName, pos, quaternion);

        if (IsMyPlayer(player))
        {
            AttachSobaekToPlayer(player);
        }

        return player;
    }

    public GameObject LocalInstantiate(PlayerEnum playerEnum)
    {
        PlayerCharacterSo selectedChar = playerCharacterArray[(int)playerEnum];
        return Instantiate(selectedChar.characterPrefabSingle, transform.position, transform.rotation);
    }

    private bool IsMyPlayer(GameObject player)
    {
        PhotonView pv = player.GetComponent<PhotonView>();
        return pv != null && pv.IsMine;
    }
    #endregion

    #region 소백이 & 소백카 설정
    private void AttachSobaekToPlayer(GameObject player)
    {
        if (!ValidateSobaekSetup())
            return;
        if (Sobaek.Instance != null)
            return;

        GameObject sobaekObj = CreateSobaek(player);
        if (sobaekObj != null)
        {
            SetupSobaekCar(sobaekObj.GetComponent<Sobaek>(), player);
        }
    }

    private bool ValidateSobaekSetup()
    {
        if (sobaekPrefab == null)
        {
            Debug.LogWarning("소백이 프리팹이 설정되지 않았습니다!");
            return false;
        }
        return true;
    }

    private GameObject CreateSobaek(GameObject player)
    {
        GameObject sobaekObj = Instantiate(sobaekPrefab);
        Sobaek sobaek = sobaekObj.GetComponent<Sobaek>();

        if (sobaek != null)
        {
            sobaek.Player = GetPlayerCameraOrRoot(player);
            return sobaekObj;
        }
        else
        {
            Debug.LogError("소백이 프리팹에 Sobaek 컴포넌트가 없습니다!");
            Destroy(sobaekObj);
            return null;
        }
    }

    private void SetupSobaekCar(Sobaek sobaek, GameObject player)
    {
        if (sobaekCarPrefab == null)
        {
            Debug.LogWarning("소백카 프리팹이 설정되지 않았습니다!");
            return;
        }

        GameObject sobaekCarObj = CreateSobaekCar(player);
        sobaek.SobaekCar = sobaekCarObj;
        currentSobaekCar = sobaekCarObj;
    }

    private GameObject CreateSobaekCar(GameObject player)
    {
        GameObject sobaekCarObj = Instantiate(sobaekCarPrefab);
        sobaekCarObj.SetActive(false);

        ConfigureSobaekCar(sobaekCarObj, player);
        return sobaekCarObj;
    }

    private void ConfigureSobaekCar(GameObject sobaekCarObj, GameObject player)
    {
        SobaekCarScript carScript = sobaekCarObj.GetComponent<SobaekCarScript>();
        if (carScript == null)
            return;

        Transform playerRoot = GetPlayerRootTransform(player);
        carScript.SetPlayer(playerRoot.gameObject);

        if (carTrack != null)
        {
            carScript.SetSplineContainer(carTrack);
        }
    }

    private Transform GetPlayerCameraOrRoot(GameObject player)
    {
        Camera playerCamera = player.GetComponentInChildren<Camera>();
        return playerCamera != null ? playerCamera.transform : player.transform;
    }

    private Transform GetPlayerRootTransform(GameObject player)
    {
        Transform current = player.transform;

        while (current.parent != null)
        {
            Transform parent = current.parent;
            if (parent.GetComponent<PhotonView>() != null)
            {
                current = parent;
            }
            else
            {
                break;
            }
        }

        return current;
    }
    #endregion

    #region 정적 메서드
    public static void StartSobaekCar()
    {
        if (currentSobaekCar == null)
        {
            Debug.LogWarning("생성된 소백카가 없습니다!");
            return;
        }

        SobaekCarScript carScript = currentSobaekCar.GetComponent<SobaekCarScript>();
        if (carScript != null)
        {
            carScript.StartTrack();
        }
        else
        {
            Debug.LogWarning("소백카에 SobaekCarScript가 없습니다!");
        }
    }
    #endregion
}
