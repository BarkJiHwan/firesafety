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
    [SerializeField] private GameObject exitSobaekPrefab;
    [SerializeField] private GameObject sobaekCarPrefab;
    [SerializeField] private SplineContainer carTrack;

    private GameObject _currentSobaekCar;
    private GameObject _currentPlayer; // CHM - 텔레포트에서 사용할 플레이어 참조

    public SplineContainer CarTrack => carTrack;
    public GameObject CurrentSobaekCar => _currentSobaekCar;
    public GameObject CurrentPlayer => _currentPlayer;

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
        if (exitSobaekPrefab == null)
        {
            exitSobaekPrefab = Resources.Load<GameObject>("ExitSobaek");
        }

        if (sobaekCarPrefab == null)
        {
            sobaekCarPrefab = Resources.Load<GameObject>("SobaekCar");
        }

        if (CarTrack == null)
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
        _currentPlayer = player;//CHM 추가
        player.GetComponent<PlayerComponents>().customTunnelingVignette.SightShrink();

        if (player != null)
        {
            AttachSobaekToPlayer(player);
        }
    }
    private bool IsTargetScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 직접 씬 이름으로 확인
        if (currentSceneName.Equals("ExitScenes_CHM.Test")
            || currentSceneName.Equals("ExitScene"))
        {
            return true;
        }

        // SceneController가 있을 때만 체크
        if (SceneController.Instance != null &&
            SceneController.Instance.chooseSceneType == SceneType.IngameScene_Evacuation)
        {
            return true;
        }

        return false;
    }
    private PlayerEnum GetSelectedCharacter()
    {
        if (SceneController.Instance != null &&
            SceneController.Instance.GetChooseCharacterType() != null)
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
        if (ExitSobaek.Instance != null)
            return;

        GameObject sobaekObj = CreateSobaek(player);
        if (sobaekObj != null)
        {
            SetupSobaekCar(sobaekObj.GetComponent<ExitSobaek>(), player);
        }
    }

    private bool ValidateSobaekSetup()
    {
        if (exitSobaekPrefab == null)
        {
            Debug.LogWarning("소백이 프리팹이 설정되지 않았습니다!");
            return false;
        }
        return true;
    }

    private GameObject CreateSobaek(GameObject player)
    {
        GameObject sobaekObj = Instantiate(exitSobaekPrefab);
        ExitSobaek sobaek = sobaekObj.GetComponent<ExitSobaek>();

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

    private void SetupSobaekCar(ExitSobaek sobaek, GameObject player)
    {
        if (sobaekCarPrefab == null)
        {
            Debug.LogWarning("소백카 프리팹이 설정되지 않았습니다!");
            return;
        }

        GameObject sobaekCarObj = CreateSobaekCar(player);
        sobaek.SobaekCar = sobaekCarObj;
        _currentSobaekCar = sobaekCarObj;
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

    //소화전 클릭시 출발 할 메서드
    public void StartSobaekCar()
    {
        if (CurrentSobaekCar == null)
        {
            Debug.LogWarning("생성된 소백카가 없습니다!");
            return;
        }

        SobaekCarScript carScript = CurrentSobaekCar.GetComponent<SobaekCarScript>();
        if (carScript != null)
        {
            carScript.StartTrack();
        }
        else
        {
            Debug.LogWarning("소백카에 SobaekCarScript가 없습니다!");
        }
    }

    // CHM 텔레 포트용 생성된 플레이어,소백이카 저장할 메서드

    public CustomTunnelingVignette GetPlayerVignette()
    {
        if (CurrentPlayer != null)
        {
            return CurrentPlayer.GetComponent<PlayerComponents>().customTunnelingVignette;
        }
        return null;
    }

    public SplineAnimate GetSobaekCarSpline()
    {
        if (CurrentSobaekCar != null)
        {
            return CurrentSobaekCar.GetComponent<SplineAnimate>();
        }
        return null;
    }
}
