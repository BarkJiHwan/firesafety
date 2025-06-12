using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 인스펙터에서 보여줄 플레이어 처치 정보
/// </summary>
[System.Serializable]
public class PlayerKillInfo
{
    public int playerID;
    public string playerName;
    public int killCount;
    public int killScore;

    public PlayerKillInfo(int id, string name, int kills, int score)
    {
        playerID = id;
        playerName = name;
        killCount = kills;
        killScore = score;
    }
}

/// <summary>
/// 인스펙터에서 보여줄 게임 점수 통합 정보
/// </summary>
[System.Serializable]
public class GameScoreInfo
{
    [Header("생존시간 점수")]
    public float maxSurvivalTime = 0f;
    public int survivalScore = 0;

    [Header("처치 통계")]
    public int totalKills = 0;
    public string topKiller = "없음";
    public int topKillerCount = 0;

    [Header("게임 상태")]
    public bool isTracking = false;
    public int playerCount = 0;
    public string gamePhase = "대기중";

    public void UpdateInfo(float survivalTime, int survivalPoints, int totalKillCount,
                          string topPlayer, int topCount, bool tracking, int players, string phase)
    {
        maxSurvivalTime = survivalTime;
        survivalScore = survivalPoints;
        totalKills = totalKillCount;
        topKiller = topPlayer;
        topKillerCount = topCount;
        isTracking = tracking;
        playerCount = players;
        gamePhase = phase;

    }
}
/// <summary>
/// 태우리 오브젝트 풀링 및 네트워크 동기화를 관리하는 매니저
/// 마스터 클라이언트가 모든 태우리 로직을 처리하고, 다른 클라이언트들은 시각적 동기화만 받음
/// </summary>
public class TaewooriPoolManager : MonoBehaviourPunCallbacks
{

    #region 인스펙터 설정
    [Header("===== 프리팹 설정 =====")]
    [SerializeField] private GameObject taewooriPrefab;
    [SerializeField] private GameObject smallTaewooriPrefab;
    [SerializeField] private GameObject fireParticlePrefab;

    [Header("===== 풀 설정 =====")]
    [SerializeField] private int initialPoolSize = 10;

    [Header("===== 리스폰 설정 =====")]
    [SerializeField] private float baseRespawnTime = 10f;
    [SerializeField] private float feverTimecCoolTime = 0.5f;

    [Header("===== 생존시간 점수 시스템 =====")]
    [SerializeField] private bool isSurvivalTracking = false; // 생존시간 추적 중인지
    [SerializeField] private float maxSurvivalTime = 0f;      // 가장 오래 살아남은 시간
    [SerializeField] private int calculatedScore = 0;         // 계산된 점수
    #endregion

    #region 변수 선언
    // 오브젝트 풀
    private Queue<GameObject> taewooriPool = new Queue<GameObject>();
    private Queue<GameObject> smallTaewooriPool = new Queue<GameObject>();
    private Queue<GameObject> fireParticlePool = new Queue<GameObject>();

    private Dictionary<Taewoori, int> smallTaewooriCountByTaewoori = new Dictionary<Taewoori, int>();

    // 네트워크 동기화용 추적
    private Dictionary<int, GameObject> networkTaewooriDict = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> networkSmallTaewooriDict = new Dictionary<int, GameObject>();
    private int nextTaewooriID = 0;
    private int nextSmallTaewooriID = 0;

    //생존시간 추적용
    private Dictionary<int, float> taewooriSpawnTimes = new Dictionary<int, float>();
    //플레이어에게 잡힌 태우리 추적
    private Dictionary<int, int> playerTaewooriKills = new Dictionary<int, int>();
    private Dictionary<int, int> playerKillScores = new Dictionary<int, int>();

    [SerializeField] private int totalTaewooriKilled = 0;                    // 전체 처치수
    [SerializeField] private List<PlayerKillInfo> playerKillInfos = new List<PlayerKillInfo>();  // 플레이어 정보 리스트  
    [SerializeField] private GameScoreInfo gameScoreInfo = new GameScoreInfo();                  // 게임 점수 정보
    /// <summary>
    /// 현재 피버타임 상태 확인
    /// </summary>
    private bool IsFeverTime => GameManager.Instance != null &&
                              GameManager.Instance.CurrentPhase == GamePhase.Fever;

    // 리스폰 관리
    private class RespawnEntry
    {
        public FireObjScript FireObj;
        public float Timer;

        public RespawnEntry(FireObjScript fireObj)
        {
            FireObj = fireObj;
            Timer = 0f;
        }
    }
    private List<RespawnEntry> respawnQueue = new List<RespawnEntry>();

    private static TaewooriPoolManager _instance;
    public static TaewooriPoolManager Instance
    {
        get { return _instance; }
    }
    #endregion

    #region 통합 생존시간 점수 시스템 프로퍼티
    /// <summary>
    /// 생존시간별 점수 (차연우씨 이거 갔다 쓰면됨)
    /// </summary>
    public int WorstSurvivalScore => calculatedScore;

    /// <summary>
    /// 현재 최대 생존시간
    /// </summary>
    public float MaxSurvivalTime => maxSurvivalTime;

    /// <summary>
    /// 생존시간 추적 중인지 여부
    /// </summary>
    public bool IsSurvivalTracking => isSurvivalTracking;

    /// <summary>
    /// 플레이어별 태우리 처치 점수 조회 어떤 플레이어가 태우리 잡은 횟수보고 점수계산되서 담겨있음 (차연우씨 이거 플레이어 개별점수임)
    /// </summary>
    public int GetPlayerKillScore(int playerID) => playerKillScores.GetValueOrDefault(playerID, 0);

    /// <summary>
    /// 플레이어별 태우리 처치 횟수 조회 어떤 플레이어가 몇마리 잡았는지 알수있음 
    /// </summary>
    public int GetPlayerKillCount(int playerID) => playerTaewooriKills.GetValueOrDefault(playerID, 0);

    /// <summary>
    /// 전체 처치된 태우리 수
    /// </summary>
    public int TotalTaewooriKilled => totalTaewooriKilled;

    /// <summary>
    /// 모든 플레이어의 태우리 잡은 횟수 담겨있음
    /// </summary>
    public Dictionary<int, int> AllPlayerKillCounts => new Dictionary<int, int>(playerTaewooriKills);

    /// <summary>
    /// 모든 플레이어의 처치된 점수 계산되서 담겨있음
    /// </summary>
    public Dictionary<int, int> AllPlayerKillScores => new Dictionary<int, int>(playerKillScores);

    /// <summary>
    /// 게임 종합 점수 정보 
    /// </summary>
    public GameScoreInfo CurrentGameScore => gameScoreInfo;
    #endregion

    #region 유니티 라이프사이클
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePools();
    }

    private void Start()
    {
        // 태우리 이벤트 구독
        Taewoori.OnTaewooriDestroyed += HandleTaewooriDestroyed;
    }

    private void OnDestroy()
    {
        Taewoori.OnTaewooriDestroyed -= HandleTaewooriDestroyed;
    }

    private void Update()
    {
        // 마스터 클라이언트만 리스폰 큐 처리
        if (PhotonNetwork.IsMasterClient)
        {
            ProcessRespawnQueue();
        }
    }
    #endregion

    #region 통합 생존시간 점수 시스템
    /// <summary>
    /// 생존시간별 점수 계산
    /// </summary>
    private int CalculateSurvivalScore(int playerCount, float survivalTime)
    {
        // 인원수별 기준시간 설정
        float maxTime, midTime;

        switch (playerCount)
        {
            case 1:
            {
                maxTime = 40f;
                midTime = 60f;
                break;
            }
            case 2:
            {
                maxTime = 30f;
                midTime = 45f;
                break;
            }
            case 3:
            {
                maxTime = 24f;
                midTime = 36f;
                break;
            }
            case 4:
            {
                maxTime = 20f;
                midTime = 30f;
                break;
            }
            case 5:
            {
                maxTime = 17f;
                midTime = 26f;
                break;
            }
            case 6:
            {
                maxTime = 15f;
                midTime = 23f;
                break;
            }
            default:
            {
                maxTime = 20f;
                midTime = 30f;
                break;
            }
        }

        if (survivalTime <= maxTime)
            return 25;
        else if (survivalTime <= midTime)
            return 20;
        else
            return 15;
    }

    /// <summary>
    /// 플레이어 태우리 처치 기록 및 점수 계산 (Fire + Fever 기간)
    /// </summary>
    public void RecordPlayerTaewooriKill(int killerPlayerID)
    {
        if (!PhotonNetwork.IsMasterClient || !isSurvivalTracking)
            return;

        // 처치 횟수 증가
        if (!playerTaewooriKills.ContainsKey(killerPlayerID))
            playerTaewooriKills[killerPlayerID] = 0;
        playerTaewooriKills[killerPlayerID]++;

        // 전체 처치 수 증가
        totalTaewooriKilled++;

        // 실시간 처치 점수 계산
        int killCount = playerTaewooriKills[killerPlayerID];
        int killScore;
        if (killCount >= 30)
            killScore = 25;      // 30마리 이상
        else if (killCount >= 24)
            killScore = 20;      // 24마리 이상
        else
            killScore = 15;      // 24마리 미만

        playerKillScores[killerPlayerID] = killScore;

        // 인스펙터 표시용 업데이트
        UpdateInspectorKillInfo();
        UpdateGameScoreInfo();

        Debug.Log($"[처치점수] 플레이어 {killerPlayerID} 태우리 처치: {killCount}마리 → {killScore}점 (전체: {totalTaewooriKilled}마리)");
    }

    /// <summary>
    /// Fire 페이즈 시작 시 호출 - 생존시간 추적 및 처치 기록 시작
    /// </summary>
    public void StartSurvivalTracking()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        isSurvivalTracking = true;
        maxSurvivalTime = 0f;
        calculatedScore = 0;
        totalTaewooriKilled = 0;
        taewooriSpawnTimes.Clear();

        // 처치 데이터 초기화
        playerTaewooriKills.Clear();
        playerKillScores.Clear();
        playerKillInfos.Clear();

        // 게임 점수 정보 초기화
        UpdateGameScoreInfo();
    }

    /// <summary>
    /// 피버타임 종료 시 호출 - 최종 생존시간 점수 계산 및 처치 점수 확정
    /// </summary>
    public void EndSurvivalTracking()
    {
        if (!PhotonNetwork.IsMasterClient || !isSurvivalTracking)
            return;

        isSurvivalTracking = false;

        // 생존시간 점수 계산
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        calculatedScore = CalculateSurvivalScore(playerCount, maxSurvivalTime);

        // 네트워크로 최종 점수 동기화 (생존시간 + 처치 점수 모두)
        photonView.RPC("SyncFinalScores", RpcTarget.All,
            maxSurvivalTime, calculatedScore,
            playerTaewooriKills.Keys.ToArray(),
            playerKillScores.Values.ToArray());

        // 최종 게임 점수 정보 업데이트
        UpdateGameScoreInfo();

        // 추적 데이터 정리
        taewooriSpawnTimes.Clear();
    }

    /// <summary>
    /// 태우리 스폰 시 시작시간 기록
    /// </summary>
    private void RecordTaewooriSpawnTime(int taewooriID)
    {
        if (!PhotonNetwork.IsMasterClient || !isSurvivalTracking)
            return;

        taewooriSpawnTimes[taewooriID] = Time.time;
    }

    /// <summary>
    /// 태우리 사망 시 생존시간 계산 및 처치자 기록 (모든 태우리는 플레이어가 처치)
    /// </summary>
    public void UpdateSurvivalTimeAndRecordKill(int taewooriID, int killerPlayerID)
    {
        if (!PhotonNetwork.IsMasterClient || !isSurvivalTracking)
            return;

        // 생존시간 계산
        if (taewooriSpawnTimes.ContainsKey(taewooriID))
        {
            float spawnTime = taewooriSpawnTimes[taewooriID];
            float survivalTime = Time.time - spawnTime;

            // 최대 생존시간 업데이트 (모든 죽은 태우리 중에서)
            if (survivalTime > maxSurvivalTime)
            {
                maxSurvivalTime = survivalTime;
                UpdateGameScoreInfo();
            }

            // 스폰시간 기록 제거 (죽었으므로)
            taewooriSpawnTimes.Remove(taewooriID);
        }

        // 처치자 기록 (모든 태우리는 반드시 처치자가 있음)
        RecordPlayerTaewooriKill(killerPlayerID);
    }

    /// <summary>
    /// 게임 리셋 시 호출 - 모든 추적 데이터 초기화
    /// </summary>
    public void ResetSurvivalTracking()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        isSurvivalTracking = false;
        maxSurvivalTime = 0f;
        calculatedScore = 0;
        totalTaewooriKilled = 0;
        taewooriSpawnTimes.Clear();

        // 처치 데이터 초기화
        playerTaewooriKills.Clear();
        playerKillScores.Clear();
        playerKillInfos.Clear();

        // 게임 점수 정보 리셋
        gameScoreInfo = new GameScoreInfo();
    }

    /// <summary>
    /// 최종 점수 네트워크 동기화 (생존시간 + 처치 점수 통합)
    /// </summary>
    [PunRPC]
    private void SyncFinalScores(float finalMaxTime, int finalSurvivalScore, int[] playerIDs, int[] killScores)
    {
        // 생존시간 점수 동기화
        maxSurvivalTime = finalMaxTime;
        calculatedScore = finalSurvivalScore;

        // 처치 점수 동기화
        playerKillScores.Clear();
        for (int i = 0; i < playerIDs.Length; i++)
        {
            playerKillScores[playerIDs[i]] = killScores[i];
        }

        // 인스펙터 정보 업데이트
        UpdateInspectorKillInfo();
        UpdateGameScoreInfo();
    }

    /// <summary>
    /// 인스펙터 표시용 플레이어 처치 정보 업데이트
    /// </summary>
    private void UpdateInspectorKillInfo()
    {
        playerKillInfos.Clear();

        foreach (var kvp in playerTaewooriKills)
        {
            int playerID = kvp.Key;
            int killCount = kvp.Value;
            int killScore = playerKillScores.GetValueOrDefault(playerID, 0);

            // 플레이어 이름 가져오기 (Photon에서)
            string playerName = "Unknown";
            var player = PhotonNetwork.CurrentRoom?.Players?.Values?.FirstOrDefault(p => p.ActorNumber == playerID);
            if (player != null)
            {
                playerName = player.NickName ?? $"Player{playerID}";
            }

            playerKillInfos.Add(new PlayerKillInfo(playerID, playerName, killCount, killScore));
        }

        // 처치 수 내림차순 정렬
        playerKillInfos.Sort((a, b) => b.killCount.CompareTo(a.killCount));
    }

    /// <summary>
    /// 인스펙터 표시용 게임 점수 종합 정보 업데이트
    /// </summary>
    private void UpdateGameScoreInfo()
    {
        // 태우리 처치 1등 찾기 (혹시 필요할까봐 만들어둠)
        string topKiller = "없음";
        int topKillerCount = 0;

        if (playerTaewooriKills.Count > 0)
        {
            var topPlayer = playerTaewooriKills.OrderByDescending(kvp => kvp.Value).First();
            topKillerCount = topPlayer.Value;

            // 플레이어 이름 가져오기
            var player = PhotonNetwork.CurrentRoom?.Players?.Values?.FirstOrDefault(p => p.ActorNumber == topPlayer.Key);
            topKiller = player?.NickName ?? $"Player{topPlayer.Key}";
        }

        // 현재 게임 페이즈 가져오기
        string currentPhase = "대기중";
        if (GameManager.Instance != null)
        {
            currentPhase = GameManager.Instance.CurrentPhase.ToString();
        }

        // 현재 플레이어 수
        int currentPlayerCount = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;

        // 게임 점수 정보 업데이트
        gameScoreInfo.UpdateInfo(
            maxSurvivalTime,
            calculatedScore,
            totalTaewooriKilled,
            topKiller,
            topKillerCount,
            isSurvivalTracking,
            currentPlayerCount,
            currentPhase
        );
    }
    #endregion

    #region 기본 풀링 시스템
    /// <summary>
    /// 오브젝트 풀 초기화 - 지정된 개수만큼 미리 생성
    /// </summary>
    private void InitializePools()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledObject(taewooriPrefab, taewooriPool);
            CreatePooledObject(smallTaewooriPrefab, smallTaewooriPool);
            CreatePooledObject(fireParticlePrefab, fireParticlePool);
        }
    }

    /// <summary>
    /// 풀에 새 오브젝트 생성 및 추가
    /// </summary>
    private GameObject CreatePooledObject(GameObject prefab, Queue<GameObject> pool)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        pool.Enqueue(obj);
        return obj;
    }

    /// <summary>
    /// 풀에서 오브젝트 가져오기 - 부족하면 새로 생성
    /// </summary>
    private GameObject GetFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        if (pool.Count == 0)
        {
            return CreatePooledObject(prefab, pool);
        }

        GameObject obj = pool.Dequeue();
        if (obj == null)
        {
            return CreatePooledObject(prefab, pool);
        }

        return obj;
    }
    #endregion

    #region 리스폰 시스템
    /// <summary>
    /// 태우리 파괴 이벤트 핸들러 - 마스터만 처리
    /// </summary>
    private void HandleTaewooriDestroyed(Taewoori taewoori, FireObjScript fireObj)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        // 생존시간 기록은 Taewoori.Die()에서 직접 처리
        // 여기서는 리스폰만 처리

        if (fireObj != null)
        {
            fireObj.ClearActiveTaewoori();
            QueueForRespawn(fireObj);
        }
    }


    /// <summary>
    /// 리스폰 큐에 화재 오브젝트 추가
    /// </summary>
    private void QueueForRespawn(FireObjScript fireObj)
    {
        if (!PhotonNetwork.IsMasterClient || fireObj == null || !fireObj.IsBurning)
            return;

        foreach (var entry in respawnQueue)
        {
            if (entry.FireObj == fireObj)
                return;
        }

        respawnQueue.Add(new RespawnEntry(fireObj));
    }

    /// <summary>
    /// 리스폰 큐 처리 - 시간 경과 후 태우리 재생성
    /// </summary>
    private void ProcessRespawnQueue()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        float respawnTime = IsFeverTime ? baseRespawnTime * feverTimecCoolTime : baseRespawnTime;

        RespawnEntry[] entries = respawnQueue.ToArray();
        List<RespawnEntry> completedEntries = new List<RespawnEntry>();

        foreach (var entry in entries)
        {
            if (entry.FireObj == null || !entry.FireObj.IsBurning)
            {
                completedEntries.Add(entry);
                continue;
            }

            entry.Timer += Time.deltaTime;

            if (entry.Timer >= respawnTime)
            {
                SpawnTaewooriAtPosition(entry.FireObj.TaewooriPos(), entry.FireObj);
                completedEntries.Add(entry);
            }
        }
        foreach (var entry in completedEntries)
        {
            respawnQueue.Remove(entry);
        }
    }
    #endregion

    #region 네트워크 스폰 함수들
    /// <summary>
    /// 태우리 생성 (마스터만 생성) - 각 태우리에게 네트워크 ID 할당 후 클라이언트에 동기화
    /// </summary>
    public GameObject SpawnTaewooriAtPosition(Vector3 position, FireObjScript fireObj)
    {
        if (!PhotonNetwork.IsMasterClient || fireObj == null || !fireObj.IsBurning)
            return null;

        if (fireObj.HasActiveTaewoori())
            return null;

        FirePreventable preventable = fireObj.GetComponent<FirePreventable>();
        if (preventable != null && preventable.IsFirePreventable)
            return null;

        GameObject taewooriObj = GetFromPool(taewooriPool, taewooriPrefab);

        if (taewooriObj != null)
        {
            Vector3 spawnPosition = fireObj.TaewooriPos();
            Quaternion spawnRotation = fireObj.TaewooriRotation();

            taewooriObj.transform.position = spawnPosition;
            taewooriObj.transform.rotation = spawnRotation;

            Taewoori taewooriComponent = taewooriObj.GetComponent<Taewoori>();
            if (taewooriComponent != null)
            {
                int taewooriID = nextTaewooriID++;
                taewooriComponent.Initialize(this, fireObj, taewooriID);
                smallTaewooriCountByTaewoori[taewooriComponent] = 0;
                networkTaewooriDict[taewooriID] = taewooriObj;

                RecordTaewooriSpawnTime(taewooriID); //생존시간 추적
            }

            taewooriObj.SetActive(true);

            // 네트워크로 태우리 생성 알림
            photonView.RPC("NetworkSpawnTaewoori", RpcTarget.Others,
                taewooriComponent.NetworkID, spawnPosition, spawnRotation);
            return taewooriObj;
        }

        return null;
    }

    /// <summary>
    /// 발사체 생성 (마스터만 생성) - 스몰태우리 생성 제한 체크 후 생성
    /// </summary>
    public GameObject PoolSpawnFireParticle(Vector3 position, Quaternion rotation, Taewoori taewoori)
    {
        if (!PhotonNetwork.IsMasterClient)
            return null;

        int currentCount = GetSmallTaewooriCount(taewoori);
        if (currentCount >= taewoori.MaxSmallTaewooriCount)
        {
            return null;
        }

        GameObject particle = GetFromPool(fireParticlePool, fireParticlePrefab);

        if (particle != null)
        {
            particle.transform.position = position;
            particle.transform.rotation = rotation;
            particle.SetActive(true);

            FireParticles particleComponent = particle.GetComponent<FireParticles>();
            if (particleComponent != null)
            {
                particleComponent.SetOriginTaewoori(taewoori);
            }

            IncrementSmallTaewooriCount(taewoori);

            // 네트워크로 발사체 생성 알림
            photonView.RPC("NetworkSpawnFireParticle", RpcTarget.Others,
                taewoori.NetworkID, position, rotation);
        }

        return particle;
    }

    /// <summary>
    /// 스몰태우리 생성 (마스터만 생성) - 각 스몰태우리에게 네트워크 ID 할당 후 클라이언트에 동기화
    /// </summary>
    public GameObject PoolSpawnSmallTaewoori(Vector3 position, Taewoori originTaewoori)
    {
        if (!PhotonNetwork.IsMasterClient || originTaewoori == null)
            return null;

        GameObject smallTaewoori = GetFromPool(smallTaewooriPool, smallTaewooriPrefab);

        if (smallTaewoori != null)
        {
            smallTaewoori.transform.position = position;
            smallTaewoori.SetActive(true);

            SmallTaewoori smallTaewooriComponent = smallTaewoori.GetComponent<SmallTaewoori>();
            if (smallTaewooriComponent != null)
            {
                int smallTaewooriID = nextSmallTaewooriID++;
                smallTaewooriComponent.InitializeWithoutCountIncrement(this, originTaewoori, smallTaewooriID);
                networkSmallTaewooriDict[smallTaewooriID] = smallTaewoori;
            }

            // 네트워크로 스몰 태우리 생성 알림
            photonView.RPC("NetworkSpawnSmallTaewoori", RpcTarget.Others,
                originTaewoori.NetworkID, position, smallTaewooriComponent.NetworkID);
        }

        return smallTaewoori;
    }
    #endregion

    #region 네트워크 RPC 수신 함수들
    /// <summary>
    /// 클라이언트용 태우리 생성 RPC - 마스터에서 보낸 데이터로 시각적 생성만
    /// </summary>
    [PunRPC]
    void NetworkSpawnTaewoori(int taewooriID, Vector3 position, Quaternion rotation)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        GameObject taewooriObj = GetFromPool(taewooriPool, taewooriPrefab);
        if (taewooriObj != null)
        {
            taewooriObj.transform.position = position;
            taewooriObj.transform.rotation = rotation;

            Taewoori taewooriComponent = taewooriObj.GetComponent<Taewoori>();
            if (taewooriComponent != null)
            {
                taewooriComponent.InitializeAsClient(taewooriID);
                networkTaewooriDict[taewooriID] = taewooriObj;
            }

            taewooriObj.SetActive(true);
        }
    }

    /// <summary>
    /// 클라이언트용 발사체 생성 RPC - 시각적 효과만 생성
    /// </summary>
    [PunRPC]
    void NetworkSpawnFireParticle(int taewooriID, Vector3 position, Quaternion rotation)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkTaewooriDict.TryGetValue(taewooriID, out GameObject taewooriObj))
        {
            GameObject particle = GetFromPool(fireParticlePool, fireParticlePrefab);
            if (particle != null)
            {
                particle.transform.position = position;
                particle.transform.rotation = rotation;
                particle.SetActive(true);

                Taewoori taewoori = taewooriObj.GetComponent<Taewoori>();
                FireParticles particleComponent = particle.GetComponent<FireParticles>();
                if (particleComponent != null)
                {
                    particleComponent.SetOriginTaewoori(taewoori);
                }
            }
        }
    }

    /// <summary>
    /// 클라이언트용 스몰태우리 생성 RPC - 시각적 생성만
    /// </summary>
    [PunRPC]
    void NetworkSpawnSmallTaewoori(int originTaewooriID, Vector3 position, int smallTaewooriID)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkTaewooriDict.TryGetValue(originTaewooriID, out GameObject originTaewooriObj))
        {
            GameObject smallTaewoori = GetFromPool(smallTaewooriPool, smallTaewooriPrefab);
            if (smallTaewoori != null)
            {
                smallTaewoori.transform.position = position;
                smallTaewoori.SetActive(true);

                Taewoori originTaewoori = originTaewooriObj.GetComponent<Taewoori>();
                SmallTaewoori smallTaewooriComponent = smallTaewoori.GetComponent<SmallTaewoori>();
                if (smallTaewooriComponent != null)
                {
                    smallTaewooriComponent.InitializeAsClient(originTaewoori, smallTaewooriID);
                    networkSmallTaewooriDict[smallTaewooriID] = smallTaewoori;
                }
            }
        }
    }

    /// <summary>
    /// 클라이언트용 태우리 데미지 동기화 RPC - 체력 정보로 색상 변경
    /// </summary>
    [PunRPC]
    void NetworkTaewooriDamage(int taewooriID, float currentHealth, float maxHealth)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkTaewooriDict.TryGetValue(taewooriID, out GameObject taewooriObj))
        {
            Taewoori taewoori = taewooriObj.GetComponent<Taewoori>();
            if (taewoori != null)
            {
                taewoori.SyncHealthFromNetwork(currentHealth, maxHealth);
            }
        }
    }

    /// <summary>
    /// 클라이언트용 스몰태우리 데미지 동기화 RPC - 체력 정보로 색상 변경
    /// </summary>
    [PunRPC]
    void NetworkSmallTaewooriDamage(int smallTaewooriID, float currentHealth, float maxHealth)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkSmallTaewooriDict.TryGetValue(smallTaewooriID, out GameObject smallTaewooriObj))
        {
            SmallTaewoori smallTaewoori = smallTaewooriObj.GetComponent<SmallTaewoori>();
            if (smallTaewoori != null)
            {
                smallTaewoori.SyncHealthFromNetwork(currentHealth, maxHealth);
            }
        }
    }

    /// <summary>
    /// 클라이언트용 태우리 파괴 동기화 RPC - 시각적 제거
    /// </summary>
    [PunRPC]
    void NetworkTaewooriDestroy(int taewooriID)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkTaewooriDict.TryGetValue(taewooriID, out GameObject taewooriObj))
        {
            Taewoori taewoori = taewooriObj.GetComponent<Taewoori>();
            if (taewoori != null)
            {
                taewoori.DieAsClient();
            }
            networkTaewooriDict.Remove(taewooriID);
        }
    }

    /// <summary>
    /// 클라이언트용 스몰태우리 파괴 동기화 RPC - 시각적 제거
    /// </summary>
    [PunRPC]
    void NetworkSmallTaewooriDestroy(int smallTaewooriID)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkSmallTaewooriDict.TryGetValue(smallTaewooriID, out GameObject smallTaewooriObj))
        {
            SmallTaewoori smallTaewoori = smallTaewooriObj.GetComponent<SmallTaewoori>();
            if (smallTaewoori != null)
            {
                smallTaewoori.DieAsClient();
            }
            networkSmallTaewooriDict.Remove(smallTaewooriID);
        }
    }

    /// <summary>
    /// 마스터용 태우리 데미지 요청 RPC - 클라이언트의 공격을 마스터가 처리
    /// </summary>
    [PunRPC]
    void RequestTaewooriDamage(int taewooriID, float damage, int senderID)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (networkTaewooriDict.TryGetValue(taewooriID, out GameObject taewooriObj))
        {
            Taewoori taewoori = taewooriObj.GetComponent<Taewoori>();
            if (taewoori != null && !taewoori.IsDead)
            {
                taewoori.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// 마스터용 스몰태우리 데미지 요청 RPC - 클라이언트의 공격을 마스터가 처리
    /// </summary>
    [PunRPC]
    void RequestSmallTaewooriDamage(int smallTaewooriID, float damage, int senderID)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (networkSmallTaewooriDict.TryGetValue(smallTaewooriID, out GameObject smallTaewooriObj))
        {
            SmallTaewoori smallTaewoori = smallTaewooriObj.GetComponent<SmallTaewoori>();
            if (smallTaewoori != null && !smallTaewoori.IsDead)
            {
                smallTaewoori.TakeDamage(damage);
            }
        }
    }

    #endregion

    #region 네트워크 동기화 헬퍼 함수들
    /// <summary>
    /// 태우리 데미지를 모든 클라이언트에 동기화
    /// </summary>
    public void SyncTaewooriDamage(int taewooriID, float currentHealth, float maxHealth)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC("NetworkTaewooriDamage", RpcTarget.Others, taewooriID, currentHealth, maxHealth);
    }

    /// <summary>
    /// 스몰태우리 데미지를 모든 클라이언트에 동기화
    /// </summary>
    public void SyncSmallTaewooriDamage(int smallTaewooriID, float currentHealth, float maxHealth)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC("NetworkSmallTaewooriDamage", RpcTarget.Others, smallTaewooriID, currentHealth, maxHealth);
    }

    /// <summary>
    /// 태우리 파괴를 모든 클라이언트에 동기화
    /// </summary>
    public void SyncTaewooriDestroy(int taewooriID)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC("NetworkTaewooriDestroy", RpcTarget.Others, taewooriID);
    }

    /// <summary>
    /// 스몰태우리 파괴를 모든 클라이언트에 동기화
    /// </summary>
    public void SyncSmallTaewooriDestroy(int smallTaewooriID)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC("NetworkSmallTaewooriDestroy", RpcTarget.Others, smallTaewooriID);
    }
    #endregion

    #region 스몰태우리 카운트 관리
    /// <summary>
    /// 특정 태우리의 스몰태우리 개수 증가
    /// </summary>
    public void IncrementSmallTaewooriCount(Taewoori originTaewoori)
    {
        if (!PhotonNetwork.IsMasterClient || originTaewoori == null)
            return;

        if (!smallTaewooriCountByTaewoori.ContainsKey(originTaewoori))
        {
            smallTaewooriCountByTaewoori[originTaewoori] = 0;
        }

        smallTaewooriCountByTaewoori[originTaewoori]++;
    }

    /// <summary>
    /// 특정 태우리의 스몰태우리 개수 감소
    /// </summary>
    public void DecrementSmallTaewooriCount(Taewoori originTaewoori)
    {
        if (!PhotonNetwork.IsMasterClient || originTaewoori == null)
            return;

        if (smallTaewooriCountByTaewoori.ContainsKey(originTaewoori) && smallTaewooriCountByTaewoori[originTaewoori] > 0)
        {
            smallTaewooriCountByTaewoori[originTaewoori]--;
        }
    }

    /// <summary>
    /// 특정 태우리의 현재 스몰태우리 개수 반환
    /// </summary>
    public int GetSmallTaewooriCount(Taewoori originTaewoori)
    {
        if (originTaewoori == null)
            return 0;

        return smallTaewooriCountByTaewoori.TryGetValue(originTaewoori, out int count) ? count : 0;
    }

    /// <summary>
    /// 발사체 생성 가능 여부 확인 (스몰태우리 최대 개수 체크)
    /// </summary>
    public bool CanLaunchProjectile(Taewoori taewoori, int maxSmallTaewooriCount)
    {
        if (!PhotonNetwork.IsMasterClient || taewoori == null)
            return false;

        int currentCount = GetSmallTaewooriCount(taewoori);
        return currentCount < maxSmallTaewooriCount;
    }
    #endregion

    #region 풀 반환 함수들
    /// <summary>
    /// 태우리를 풀로 반환 - 네트워크 딕셔너리에서도 제거
    /// </summary>
    public void ReturnTaewooriToPool(GameObject taewooriObj)
    {
        if (taewooriObj == null)
            return;

        Taewoori taewoori = taewooriObj.GetComponent<Taewoori>();
        if (taewoori != null)
        {
            // 네트워크 딕셔너리에서 제거
            if (networkTaewooriDict.ContainsValue(taewooriObj))
            {
                var keyToRemove = -1;
                foreach (var kvp in networkTaewooriDict)
                {
                    if (kvp.Value == taewooriObj)
                    {
                        keyToRemove = kvp.Key;
                        break;
                    }
                }
                if (keyToRemove != -1)
                {
                    networkTaewooriDict.Remove(keyToRemove);
                }
            }

            if (smallTaewooriCountByTaewoori.ContainsKey(taewoori))
            {
                smallTaewooriCountByTaewoori.Remove(taewoori);
            }
        }

        taewooriObj.SetActive(false);
        taewooriObj.transform.SetParent(transform);
        taewooriPool.Enqueue(taewooriObj);
    }

    /// <summary>
    /// 스몰태우리를 풀로 반환 - 네트워크 딕셔너리에서도 제거
    /// </summary>
    public void ReturnSmallTaewooriToPool(GameObject smallTaewooriObj)
    {
        if (smallTaewooriObj == null)
            return;

        SmallTaewoori smallTaewoori = smallTaewooriObj.GetComponent<SmallTaewoori>();
        if (smallTaewoori != null)
        {
            // 네트워크 딕셔너리에서 제거
            if (networkSmallTaewooriDict.ContainsValue(smallTaewooriObj))
            {
                var keyToRemove = -1;
                foreach (var kvp in networkSmallTaewooriDict)
                {
                    if (kvp.Value == smallTaewooriObj)
                    {
                        keyToRemove = kvp.Key;
                        break;
                    }
                }
                if (keyToRemove != -1)
                {
                    networkSmallTaewooriDict.Remove(keyToRemove);
                }
            }
        }

        smallTaewooriObj.SetActive(false);
        smallTaewooriObj.transform.SetParent(transform);
        smallTaewooriPool.Enqueue(smallTaewooriObj);
    }

    /// <summary>
    /// 발사체를 풀로 반환
    /// </summary>
    public void ReturnFireParticleToPool(GameObject particleObj)
    {
        if (particleObj != null)
        {
            particleObj.SetActive(false);
            particleObj.transform.SetParent(transform);
            fireParticlePool.Enqueue(particleObj);
        }
    }

    /// <summary>
    /// 발사체를 스몰태우리 생성 없이 풀로 반환 (Shield 충돌 등)
    /// </summary>
    public void ReturnFireParticleToPoolWithoutSpawn(GameObject particleObj, Taewoori originTaewoori)
    {
        if (particleObj != null)
        {
            if (PhotonNetwork.IsMasterClient && originTaewoori != null)
            {
                DecrementSmallTaewooriCount(originTaewoori);
            }

            particleObj.SetActive(false);
            particleObj.transform.SetParent(transform);
            fireParticlePool.Enqueue(particleObj);
        }
    }
    #endregion

}
