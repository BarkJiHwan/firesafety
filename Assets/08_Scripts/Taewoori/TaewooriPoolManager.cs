using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class ReadOnlyAttribute : PropertyAttribute { }

[System.Serializable]
public class PlayerKillDebugInfo
{
    [ReadOnly] public string playerName;
    [ReadOnly] public int playerID;
    [ReadOnly] public int killCount;
    [ReadOnly] public string masterStatus;
    [ReadOnly] public string localStatus;

    public override string ToString()
    {
        string prefix = masterStatus == "Master" ? "[M]" : "";
        prefix += localStatus == "Local" ? "[L]" : "";
        return $"{prefix} {playerName} (ID:{playerID}) - {killCount}킬";
    }
}

/// <summary>
/// 단순화된 태우리 오브젝트 풀링 및 네트워크 동기화를 관리하는 매니저
/// 마스터 클라이언트가 모든 태우리 로직을 처리하고, 다른 클라이언트들은 시각적 동기화만 받음
/// </summary>
public class TaewooriPoolManager : MonoBehaviourPunCallbacks
{
    #region 인스펙터 디버깅
    #region 인스펙터 디버깅
    [Header("===== 디버깅 정보 (ReadOnly) =====")]
    [SerializeField, ReadOnly] private int debugCurrentAliveTaewoori = 0;    // 현재 살아있는 태우리 개수
    [SerializeField, ReadOnly] private int debugTotalSpawnedTaewoori = 0;    // 게임 시작부터 스폰된 총 태우리 개수
    [SerializeField, ReadOnly] private int debugTotalKilledTaewoori = 0;     // 지금까지 처치된 태우리 총 개수
    [SerializeField, ReadOnly] private string debugSurvivalConfig = "";      // 현재 플레이어 수에 따른 생존시간 기준 (만기준/중간시간)
    [SerializeField, ReadOnly] private float debugMaxSurvivalTime = 0f;      // 전체 게임에서 가장 오래 살아남은 태우리의 생존시간(초)
    #endregion

    [Header("===== 플레이어별 킬 카운트 =====")]
    [SerializeField] private PlayerKillDebugInfo[] debugPlayerKills = new PlayerKillDebugInfo[0];
    #endregion

    #region 인스펙터 설정
    [Header("===== 프리팹 설정 =====")]
    [SerializeField] private GameObject taewooriPrefab;
    [SerializeField] private GameObject smallTaewooriPrefab;
    [SerializeField] private GameObject fireParticlePrefab;

    [Header("===== 풀 설정 =====")]
    [SerializeField] private int initialPoolSize = 10;

    [Header("===== 리스폰 설정 =====")]
    [SerializeField] private float baseRespawnTime = 10f;
    [SerializeField] private float feverTimeCoolTime = 0.5f;

    [Header("===== 스코어 매니저 연결 =====")]
    [SerializeField] private ScoreManager scoreManager;
    #endregion

    #region 기존 변수 선언
    // 오브젝트 풀
    private Queue<GameObject> taewooriPool = new Queue<GameObject>();
    private Queue<GameObject> smallTaewooriPool = new Queue<GameObject>();
    private Queue<GameObject> fireParticlePool = new Queue<GameObject>();

    // 단순화된 킬 카운트 (모든 클라이언트 동기화)
    private Dictionary<int, int> playerKillCounts = new Dictionary<int, int>();

    // 스몰태우리 개수 관리
    private Dictionary<Taewoori, int> smallTaewooriCountByTaewoori = new Dictionary<Taewoori, int>();

    // 네트워크 동기화용 추적
    private Dictionary<int, GameObject> networkTaewooriDict = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> networkSmallTaewooriDict = new Dictionary<int, GameObject>();
    private int nextTaewooriID = 0;
    private int nextSmallTaewooriID = 0;

    // 리스폰 관리
    private class RespawnEntry
    {
        public FireObjScript FireObj;
        public float Timer;
        public RespawnEntry(FireObjScript fireObj) { FireObj = fireObj; Timer = 0f; }
    }
    private List<RespawnEntry> respawnQueue = new List<RespawnEntry>();

    private static TaewooriPoolManager _instance;
    public static TaewooriPoolManager Instance => _instance;
    public event Action OnScoreBoardOn;
    #endregion

    #region 태우리 생존시간 추적 시스템 추가
    [Header("===== 태우리 생존시간 설정 =====")]
    [SerializeField] private bool enableSurvivalScoring = true;

    // 생존시간별 점수 설정
    [System.Serializable]
    public class SurvivalTimeScore
    {
        public int playerCount;
        public float manQiTime;      // 만 기준 시간 (초)
        public float mediumTime;     // 중간 시간 (초)
        public int maxScore = 25;    // 최대 점수
        public int mediumScore = 20; // 중간 점수
        public int minScore = 15;    // 최소 점수
    }

    [SerializeField]
    private SurvivalTimeScore[] survivalTimeScores = new SurvivalTimeScore[]
    {
        new SurvivalTimeScore { playerCount = 1, manQiTime = 40f, mediumTime = 60f },
        new SurvivalTimeScore { playerCount = 2, manQiTime = 30f, mediumTime = 45f },
        new SurvivalTimeScore { playerCount = 3, manQiTime = 24f, mediumTime = 36f },
        new SurvivalTimeScore { playerCount = 4, manQiTime = 20f, mediumTime = 30f },
        new SurvivalTimeScore { playerCount = 5, manQiTime = 17f, mediumTime = 26f },
        new SurvivalTimeScore { playerCount = 6, manQiTime = 15f, mediumTime = 23f }
    };

    // 태우리 생존시간 추적 데이터
    private Dictionary<int, float> taewooriSpawnTimes = new Dictionary<int, float>();
    private Dictionary<int, List<float>> playerTaewooriSurvivalTimes = new Dictionary<int, List<float>>();


    #endregion

    #region 프로퍼티
    private bool IsFeverTime => GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.Fever;

    public int CurrentAliveTaewooriCount
    {
        get
        {
            int aliveCount = 0;
            foreach (var kvp in networkTaewooriDict)
            {
                if (kvp.Value != null && kvp.Value.activeInHierarchy)
                {
                    var taewoori = kvp.Value.GetComponent<Taewoori>();
                    if (taewoori != null && !taewoori.IsDead)
                        aliveCount++;
                }
            }
            return aliveCount;
        }
    }
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
        InitializeComponents();
    }

    private void Start()
    {
        Taewoori.OnTaewooriDestroyed += HandleTaewooriDestroyed;
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
            Debug.Log($"ScoreManager 찾기 결과: {scoreManager}");
        }
    }

    private void OnDestroy()
    {
        Taewoori.OnTaewooriDestroyed -= HandleTaewooriDestroyed;
        CleanupAllResources();
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
            ProcessRespawnQueue();
        UpdateDebugInfo();
    }
    #endregion

    #region 초기화
    private void InitializeComponents()
    {
        InitializePools();
        if (scoreManager == null)
            scoreManager = FindObjectOfType<ScoreManager>();
    }

    private void InitializePools()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledObject(taewooriPrefab, taewooriPool);
            CreatePooledObject(smallTaewooriPrefab, smallTaewooriPool);
            CreatePooledObject(fireParticlePrefab, fireParticlePool);
        }
    }

    private GameObject CreatePooledObject(GameObject prefab, Queue<GameObject> pool)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        pool.Enqueue(obj);
        return obj;
    }

    private GameObject GetFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        if (pool.Count == 0)
            return CreatePooledObject(prefab, pool);
        GameObject obj = pool.Dequeue();
        if (obj == null)
            return CreatePooledObject(prefab, pool);
        return obj;
    }
    #endregion

    #region 태우리 생존시간 추적 시스템
    /// <summary>
    /// 전체 게임에서 가장 오래 살아남은 태우리의 생존시간 구하기
    /// </summary>
    private float GetGlobalMaxSurvivalTime()
    {
        float globalMaxTime = 0f;

        foreach (var kvp in playerTaewooriSurvivalTimes)
        {
            if (kvp.Value.Count > 0)
            {
                float playerMaxTime = Mathf.Max(kvp.Value.ToArray());
                if (playerMaxTime > globalMaxTime)
                    globalMaxTime = playerMaxTime;
            }
        }

        return globalMaxTime;
    }
    /// <summary>
    /// 전체 최대 생존시간 기반으로 모든 플레이어에게 동일한 점수 적용
    /// </summary>
    private int CalculateGlobalSurvivalScore()
    {
        float globalMaxSurvivalTime = GetGlobalMaxSurvivalTime();
        return CalculateSurvivalScore(globalMaxSurvivalTime);
    }
    /// <summary>
    /// 현재 플레이어 수에 따른 생존시간 기준 가져오기
    /// </summary>
    private SurvivalTimeScore GetCurrentSurvivalTimeScore()
    {
        int currentPlayerCount = PhotonNetwork.PlayerList.Length;

        for (int i = 0; i < survivalTimeScores.Length; i++)
        {
            if (survivalTimeScores[i].playerCount == currentPlayerCount)
            {
                return survivalTimeScores[i];
            }
        }

        return survivalTimeScores[survivalTimeScores.Length - 1];
    }

    /// <summary>
    /// 태우리 스폰 시 생존시간 추적 시작
    /// </summary>
    private void StartTaewooriSurvivalTracking(int taewooriID)
    {
        if (!PhotonNetwork.IsMasterClient || !enableSurvivalScoring)
            return;

        taewooriSpawnTimes[taewooriID] = Time.time;
        Debug.Log($"태우리 {taewooriID} 생존시간 추적 시작");
    }

    /// <summary>
    /// 태우리 사망 시 생존시간 기록
    /// </summary>
    private void RecordTaewooriSurvival(int taewooriID, int killerPlayerID)
    {
        if (!PhotonNetwork.IsMasterClient || !enableSurvivalScoring)
            return;

        if (!taewooriSpawnTimes.ContainsKey(taewooriID))
        {
            Debug.LogWarning($"태우리 {taewooriID}의 스폰시간을 찾을 수 없습니다!");
            return;
        }

        float spawnTime = taewooriSpawnTimes[taewooriID];
        float survivalTime = Time.time - spawnTime;

        if (!playerTaewooriSurvivalTimes.ContainsKey(killerPlayerID))
            playerTaewooriSurvivalTimes[killerPlayerID] = new List<float>();

        playerTaewooriSurvivalTimes[killerPlayerID].Add(survivalTime);

        Debug.Log($"태우리 {taewooriID} 생존시간 기록: {survivalTime:F1}초 (처치자: 플레이어{killerPlayerID})");

        taewooriSpawnTimes.Remove(taewooriID);
    }

    /// <summary>
    /// 생존시간에 따른 점수 계산
    /// </summary>
    private int CalculateSurvivalScore(float survivalTimeSeconds)
    {
        SurvivalTimeScore scoreConfig = GetCurrentSurvivalTimeScore();

        if (survivalTimeSeconds >= scoreConfig.mediumTime)
            return scoreConfig.maxScore;
        else if (survivalTimeSeconds >= scoreConfig.manQiTime)
            return scoreConfig.mediumScore;
        else
            return scoreConfig.minScore;
    }
    #endregion

    #region 수정된 킬 시스템
    /// <summary>
    /// 태우리 처치 시 호출 - 생존시간 기록 추가
    /// </summary>
    public void OnTaewooriKilled(int killerPlayerID, int taewooriID)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Debug.Log($"태우리 {taewooriID} 처치! 킬러: {killerPlayerID}");

        // 태우리 생존시간 기록 추가
        RecordTaewooriSurvival(taewooriID, killerPlayerID);

        // 기존 킬 카운트 증가 로직
        if (!playerKillCounts.ContainsKey(killerPlayerID))
            playerKillCounts[killerPlayerID] = 0;
        playerKillCounts[killerPlayerID]++;

        // 모든 클라이언트에 동기화
        photonView.RPC("SyncKillCount", RpcTarget.All, killerPlayerID, playerKillCounts[killerPlayerID]);
    }

    /// <summary>
    /// 수정된 게임 종료 시 점수 계산 - 모든 플레이어 동일한 생존시간 점수
    /// </summary>
    public void CalculateFinalScores()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Debug.Log("게임 종료! 전체 최대 태우리 생존시간 기반 점수 계산");

        // 전체 게임에서 가장 오래 살아남은 태우리의 생존시간 구하기
        float globalMaxSurvivalTime = GetGlobalMaxSurvivalTime();
        int globalSurvivalScore = enableSurvivalScoring ?
            CalculateSurvivalScore(globalMaxSurvivalTime) : 25;

        Debug.Log($"전체 최대 태우리 생존시간: {globalMaxSurvivalTime:F1}초 = {globalSurvivalScore}점 (모든 플레이어 공통)");

        foreach (var player in PhotonNetwork.PlayerList)
        {
            int killCount = playerKillCounts.TryGetValue(player.ActorNumber, out int kills) ? kills : 0;
            int killScore = CalculateKillScore(killCount);

            // 모든 플레이어가 동일한 생존시간 점수 받음
            int survivalScore = globalSurvivalScore;

            Debug.Log($"플레이어 {player.ActorNumber}: {killCount}킬 = {killScore}점, 생존시간점수 = {survivalScore}점 (공통)");

            // 개별 플레이어 생존시간 정보도 로그 출력
            if (playerTaewooriSurvivalTimes.ContainsKey(player.ActorNumber))
            {
                var times = playerTaewooriSurvivalTimes[player.ActorNumber];
                float playerMaxTime = times.Count > 0 ? Mathf.Max(times.ToArray()) : 0f;
                Debug.Log($"  - 플레이어 {player.ActorNumber} 개별 최대 생존시간: {playerMaxTime:F1}초, 처치 수: {times.Count}개");
            }

            // 점수 전송
            Debug.Log($"점수 RPC 발송 - 플레이어 {player.ActorNumber}: 생존={survivalScore}, 킬={killScore}");
            photonView.RPC("SetPlayerScores", RpcTarget.All, player.ActorNumber, survivalScore, killScore);
        }

        // 점수판 표시
        photonView.RPC("ShowScoreBoardRPC", RpcTarget.All);
    }

    /// <summary>
    /// 킬 점수 계산
    /// </summary>
    private int CalculateKillScore(int killCount)
    {
        if (killCount >= 2)
            return 25;
        if (killCount >= 1)
            return 20;
        return 15;
    }

    /// <summary>
    /// 게임 리셋
    /// </summary>
    public void ResetKillCounts()
    {
        playerKillCounts.Clear();

        // 생존시간 데이터도 함께 리셋
        taewooriSpawnTimes.Clear();
        playerTaewooriSurvivalTimes.Clear();

        Debug.Log("킬 카운트 및 생존시간 데이터 리셋 완료");
    }
    #endregion

    #region 리스폰 시스템
    private void HandleTaewooriDestroyed(Taewoori taewoori, FireObjScript fireObj)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (fireObj != null)
        {
            fireObj.ClearActiveTaewoori();
            QueueForRespawn(fireObj);
        }
    }

    private void QueueForRespawn(FireObjScript fireObj)
    {
        if (!PhotonNetwork.IsMasterClient || fireObj == null || !fireObj.IsBurning)
            return;

        if (respawnQueue.Any(entry => entry.FireObj == fireObj))
            return;

        respawnQueue.Add(new RespawnEntry(fireObj));
    }

    private void ProcessRespawnQueue()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        float respawnTime = IsFeverTime ? baseRespawnTime * feverTimeCoolTime : baseRespawnTime;
        var completedEntries = new List<RespawnEntry>();

        foreach (var entry in respawnQueue)
        {
            if (entry.FireObj == null || !entry.FireObj.IsBurning)
            {
                completedEntries.Add(entry);
                continue;
            }

            entry.Timer += Time.deltaTime;

            if (entry.Timer >= respawnTime)
            {
                SpawnTaewoori(entry.FireObj.TaewooriPos(), entry.FireObj);
                completedEntries.Add(entry);
            }
        }

        foreach (var entry in completedEntries)
        {
            respawnQueue.Remove(entry);
        }
    }
    #endregion

    #region 스폰 함수들
    public GameObject SpawnTaewoori(Vector3 position, FireObjScript fireObj)
    {
        if (!PhotonNetwork.IsMasterClient || !CanSpawnTaewoori(fireObj))
            return null;

        GameObject taewooriObj = GetFromPool(taewooriPool, taewooriPrefab);
        if (taewooriObj == null)
            return null;

        SetupTaewoori(taewooriObj, fireObj);
        photonView.RPC("NetworkSpawnTaewoori", RpcTarget.Others,
            taewooriObj.GetComponent<Taewoori>().NetworkID,
            fireObj.TaewooriPos(),
            fireObj.TaewooriRotation());

        return taewooriObj;
    }

    private bool CanSpawnTaewoori(FireObjScript fireObj)
    {
        if (fireObj == null || !fireObj.IsBurning || fireObj.HasActiveTaewoori())
            return false;

        var preventable = fireObj.GetComponent<FirePreventable>();
        return preventable == null || !preventable.IsFirePreventable;
    }

    private void SetupTaewoori(GameObject taewooriObj, FireObjScript fireObj)
    {
        taewooriObj.transform.position = fireObj.TaewooriPos();
        taewooriObj.transform.rotation = fireObj.TaewooriRotation();

        var taewooriComponent = taewooriObj.GetComponent<Taewoori>();
        if (taewooriComponent != null)
        {
            int taewooriID = nextTaewooriID++;
            taewooriComponent.Initialize(this, fireObj, taewooriID);
            smallTaewooriCountByTaewoori[taewooriComponent] = 0;
            networkTaewooriDict[taewooriID] = taewooriObj;

            // 생존시간 추적 시작 추가
            StartTaewooriSurvivalTracking(taewooriID);
        }

        taewooriObj.SetActive(true);
    }

    public GameObject PoolSpawnFireParticle(Vector3 position, Quaternion rotation, Taewoori taewoori)
    {
        if (!PhotonNetwork.IsMasterClient || !CanLaunchProjectile(taewoori, taewoori.MaxSmallTaewooriCount))
            return null;

        GameObject particle = GetFromPool(fireParticlePool, fireParticlePrefab);
        if (particle == null)
            return null;

        SetupFireParticle(particle, position, rotation, taewoori);
        photonView.RPC("NetworkSpawnFireParticle", RpcTarget.Others, taewoori.NetworkID, position, rotation);

        return particle;
    }

    private void SetupFireParticle(GameObject particle, Vector3 position, Quaternion rotation, Taewoori taewoori)
    {
        particle.transform.position = position;
        particle.transform.rotation = rotation;
        particle.SetActive(true);

        var particleComponent = particle.GetComponent<FireParticles>();
        if (particleComponent != null)
        {
            particleComponent.SetOriginTaewoori(taewoori);
        }

        IncrementSmallTaewooriCount(taewoori);
    }

    public GameObject PoolSpawnSmallTaewoori(Vector3 position, Taewoori originTaewoori)
    {
        if (!PhotonNetwork.IsMasterClient || originTaewoori == null)
            return null;

        GameObject smallTaewoori = GetFromPool(smallTaewooriPool, smallTaewooriPrefab);
        if (smallTaewoori == null)
            return null;

        SetupSmallTaewoori(smallTaewoori, position, originTaewoori);
        photonView.RPC("NetworkSpawnSmallTaewoori", RpcTarget.Others,
            originTaewoori.NetworkID, position,
            smallTaewoori.GetComponent<SmallTaewoori>().NetworkID);

        return smallTaewoori;
    }

    private void SetupSmallTaewoori(GameObject smallTaewoori, Vector3 position, Taewoori originTaewoori)
    {
        smallTaewoori.transform.position = position;
        smallTaewoori.SetActive(true);

        var smallTaewooriComponent = smallTaewoori.GetComponent<SmallTaewoori>();
        if (smallTaewooriComponent != null)
        {
            int smallTaewooriID = nextSmallTaewooriID++;
            smallTaewooriComponent.InitializeWithoutCountIncrement(this, originTaewoori, smallTaewooriID);
            networkSmallTaewooriDict[smallTaewooriID] = smallTaewoori;
        }
    }
    #endregion

    #region 네트워크 RPC
    [PunRPC]
    void SyncKillCount(int playerID, int totalKills)
    {
        playerKillCounts[playerID] = totalKills;
        Debug.Log($"킬카운트 동기화! 플레이어 {playerID}: {totalKills}킬");
    }

    [PunRPC]
    void SetPlayerScores(int playerId, int survivalScore, int killScore)
    {
        Debug.Log($"점수 RPC 받음 - 플레이어 {playerId}: 생존={survivalScore}, 킬={killScore}");

        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId && scoreManager != null)
        {
            scoreManager.SetScore(ScoreType.Fire_Time, survivalScore);
            scoreManager.SetScore(ScoreType.Fire_Count, killScore);
            Debug.Log($"점수 설정 완료! 플레이어 {playerId}");
        }
    }

    [PunRPC]
    void ShowScoreBoardRPC()
    {
        Debug.Log("점수판 표시!");
        OnScoreBoardOn?.Invoke();
    }

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

            var taewooriComponent = taewooriObj.GetComponent<Taewoori>();
            if (taewooriComponent != null)
            {
                taewooriComponent.InitializeAsClient(taewooriID);
                networkTaewooriDict[taewooriID] = taewooriObj;
            }

            taewooriObj.SetActive(true);
        }
    }

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

                var taewoori = taewooriObj.GetComponent<Taewoori>();
                var particleComponent = particle.GetComponent<FireParticles>();
                if (particleComponent != null)
                {
                    particleComponent.SetOriginTaewoori(taewoori);
                }
            }
        }
    }

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

                var originTaewoori = originTaewooriObj.GetComponent<Taewoori>();
                var smallTaewooriComponent = smallTaewoori.GetComponent<SmallTaewoori>();
                if (smallTaewooriComponent != null)
                {
                    smallTaewooriComponent.InitializeAsClient(originTaewoori, smallTaewooriID);
                    networkSmallTaewooriDict[smallTaewooriID] = smallTaewoori;
                }
            }
        }
    }

    [PunRPC]
    void NetworkTaewooriDamage(int taewooriID, float currentHealth, float maxHealth)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkTaewooriDict.TryGetValue(taewooriID, out GameObject taewooriObj))
        {
            var taewoori = taewooriObj.GetComponent<Taewoori>();
            if (taewoori != null)
            {
                taewoori.SyncHealthFromNetwork(currentHealth, maxHealth);
            }
        }
    }

    [PunRPC]
    void NetworkSmallTaewooriDamage(int smallTaewooriID, float currentHealth, float maxHealth)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkSmallTaewooriDict.TryGetValue(smallTaewooriID, out GameObject smallTaewooriObj))
        {
            var smallTaewoori = smallTaewooriObj.GetComponent<SmallTaewoori>();
            if (smallTaewoori != null)
            {
                smallTaewoori.SyncHealthFromNetwork(currentHealth, maxHealth);
            }
        }
    }

    [PunRPC]
    void NetworkTaewooriDestroy(int taewooriID)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkTaewooriDict.TryGetValue(taewooriID, out GameObject taewooriObj))
        {
            var taewoori = taewooriObj.GetComponent<Taewoori>();
            if (taewoori != null)
            {
                taewoori.DieAsClient();
            }
            networkTaewooriDict.Remove(taewooriID);
        }
    }

    [PunRPC]
    void NetworkSmallTaewooriDestroy(int smallTaewooriID)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkSmallTaewooriDict == null)
        {
            Debug.LogWarning("networkSmallTaewooriDict가 null입니다!");
            return;
        }

        if (networkSmallTaewooriDict.TryGetValue(smallTaewooriID, out GameObject smallTaewooriObj))
        {
            if (smallTaewooriObj == null)
            {
                Debug.LogWarning($"SmallTaewoori GameObject가 null입니다! ID: {smallTaewooriID}");
                networkSmallTaewooriDict.Remove(smallTaewooriID);
                return;
            }

            var smallTaewoori = smallTaewooriObj.GetComponent<SmallTaewoori>();
            if (smallTaewoori != null)
            {
                smallTaewoori.DieAsClient();
            }
            else
            {
                Debug.LogWarning($"SmallTaewoori 컴포넌트를 찾을 수 없습니다! GameObject: {smallTaewooriObj.name}");
            }

            networkSmallTaewooriDict.Remove(smallTaewooriID);
        }
        else
        {
            Debug.LogWarning($"SmallTaewoori ID {smallTaewooriID}를 Dictionary에서 찾을 수 없습니다!");
        }
    }

    [PunRPC]
    void RequestTaewooriDamage(int taewooriID, float damage, int senderID)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Debug.Log($"[RequestTaewooriDamage 수신] 태우리ID: {taewooriID}, 데미지: {damage}, 공격자ID: {senderID}");

        if (networkTaewooriDict.TryGetValue(taewooriID, out GameObject taewooriObj))
        {
            var taewoori = taewooriObj.GetComponent<Taewoori>();
            if (taewoori != null && !taewoori.IsDead)
            {
                Debug.Log($"[데미지 처리 전] 현재 lastAttackerID: {taewoori.GetLastAttackerID()}");
                Debug.Log($"[SetLastAttacker 호출] 새로운 공격자ID: {senderID}");

                taewoori.SetLastAttacker(senderID);

                Debug.Log($"[데미지 처리 후] 변경된 lastAttackerID: {taewoori.GetLastAttackerID()}");

                taewoori.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"[데미지 처리 실패] 태우리가 null이거나 이미 죽음");
            }
        }
        else
        {
            Debug.LogWarning($"[데미지 처리 실패] 태우리ID {taewooriID}를 찾을 수 없음");
        }
    }

    [PunRPC]
    void RequestSmallTaewooriDamage(int smallTaewooriID, float damage, int senderID)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (networkSmallTaewooriDict.TryGetValue(smallTaewooriID, out GameObject smallTaewooriObj))
        {
            var smallTaewoori = smallTaewooriObj.GetComponent<SmallTaewoori>();
            if (smallTaewoori != null && !smallTaewoori.IsDead)
            {
                smallTaewoori.TakeDamage(damage);
            }
        }
    }

    [PunRPC]
    void NetworkTaewooriHit(int taewooriID)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkTaewooriDict.TryGetValue(taewooriID, out GameObject taewooriObj))
        {
            var taewoori = taewooriObj.GetComponent<Taewoori>();
            if (taewoori != null && taewoori.UseAnimation)
            {
                taewoori.PlayHitAnimation();
            }
        }
    }

    [PunRPC]
    void NetworkTaewooriDie(int taewooriID)
    {
        if (PhotonNetwork.IsMasterClient)
            return;

        if (networkTaewooriDict.TryGetValue(taewooriID, out GameObject taewooriObj))
        {
            var taewoori = taewooriObj.GetComponent<Taewoori>();
            if (taewoori != null && taewoori.UseAnimation)
            {
                taewoori.PlayDeathAnimation();
                taewoori.StartCoroutine(taewoori.HandleDeathSequence());
            }
        }
    }
    #endregion

    #region 네트워크 동기화 헬퍼
    public void SyncTaewooriDamage(int taewooriID, float currentHealth, float maxHealth)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("NetworkTaewooriDamage", RpcTarget.Others, taewooriID, currentHealth, maxHealth);
        }
    }

    public void SyncSmallTaewooriDamage(int smallTaewooriID, float currentHealth, float maxHealth)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("NetworkSmallTaewooriDamage", RpcTarget.Others, smallTaewooriID, currentHealth, maxHealth);
        }
    }

    public void SyncTaewooriDestroy(int taewooriID)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("NetworkTaewooriDestroy", RpcTarget.Others, taewooriID);
        }
    }

    public void SyncSmallTaewooriDestroy(int smallTaewooriID)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("NetworkSmallTaewooriDestroy", RpcTarget.Others, smallTaewooriID);
        }
    }
    #endregion

    #region 스몰태우리 카운트 관리
    public void IncrementSmallTaewooriCount(Taewoori originTaewoori)
    {
        if (PhotonNetwork.IsMasterClient && originTaewoori != null)
        {
            if (!smallTaewooriCountByTaewoori.ContainsKey(originTaewoori))
            {
                smallTaewooriCountByTaewoori[originTaewoori] = 0;
            }
            smallTaewooriCountByTaewoori[originTaewoori]++;
        }
    }

    public void DecrementSmallTaewooriCount(Taewoori originTaewoori)
    {
        if (PhotonNetwork.IsMasterClient && originTaewoori != null)
        {
            if (smallTaewooriCountByTaewoori.ContainsKey(originTaewoori) &&
                smallTaewooriCountByTaewoori[originTaewoori] > 0)
            {
                smallTaewooriCountByTaewoori[originTaewoori]--;
            }
        }
    }

    public int GetSmallTaewooriCount(Taewoori originTaewoori)
    {
        if (originTaewoori == null)
            return 0;
        return smallTaewooriCountByTaewoori.TryGetValue(originTaewoori, out int count) ? count : 0;
    }

    public bool CanLaunchProjectile(Taewoori taewoori, int maxSmallTaewooriCount)
    {
        if (!PhotonNetwork.IsMasterClient || taewoori == null)
            return false;
        return GetSmallTaewooriCount(taewoori) < maxSmallTaewooriCount;
    }
    #endregion

    #region 풀 반환
    public void ReturnTaewooriToPool(GameObject taewooriObj)
    {
        if (taewooriObj == null)
            return;

        var taewoori = taewooriObj.GetComponent<Taewoori>();
        if (taewoori != null)
        {
            RemoveFromNetworkDict(taewooriObj, networkTaewooriDict);
            smallTaewooriCountByTaewoori.Remove(taewoori);
        }

        ReturnToPool(taewooriObj, taewooriPool);
    }

    public void ReturnSmallTaewooriToPool(GameObject smallTaewooriObj)
    {
        if (smallTaewooriObj == null)
            return;

        RemoveFromNetworkDict(smallTaewooriObj, networkSmallTaewooriDict);
        ReturnToPool(smallTaewooriObj, smallTaewooriPool);
    }

    public void ReturnFireParticleToPool(GameObject particleObj)
    {
        if (particleObj != null)
        {
            ReturnToPool(particleObj, fireParticlePool);
        }
    }

    public void ReturnFireParticleToPoolWithoutSpawn(GameObject particleObj, Taewoori originTaewoori)
    {
        if (particleObj != null)
        {
            if (PhotonNetwork.IsMasterClient && originTaewoori != null)
            {
                DecrementSmallTaewooriCount(originTaewoori);
            }
            ReturnToPool(particleObj, fireParticlePool);
        }
    }

    private void ReturnToPool(GameObject obj, Queue<GameObject> pool)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pool.Enqueue(obj);
    }

    private void RemoveFromNetworkDict(GameObject obj, Dictionary<int, GameObject> dict)
    {
        var keyToRemove = dict.FirstOrDefault(kvp => kvp.Value == obj).Key;
        if (keyToRemove != 0)
        {
            dict.Remove(keyToRemove);
        }
    }
    #endregion

    #region 디버깅
    private void UpdateDebugInfo()
    {
        debugCurrentAliveTaewoori = CurrentAliveTaewooriCount;
        debugTotalSpawnedTaewoori = nextTaewooriID;

        if (PhotonNetwork.IsMasterClient)
        {
            // 생존시간 디버깅 정보도 여기서 함께 처리
            var config = GetCurrentSurvivalTimeScore();
            debugSurvivalConfig = $"{PhotonNetwork.PlayerList.Length}명: 만기준{config.manQiTime}초, 중간{config.mediumTime}초";

            debugTotalKilledTaewoori = 0;
            debugMaxSurvivalTime = 0f;

            foreach (var kvp in playerTaewooriSurvivalTimes)
            {
                debugTotalKilledTaewoori += kvp.Value.Count;
                if (kvp.Value.Count > 0)
                {
                    float playerMaxTime = Mathf.Max(kvp.Value.ToArray());
                    if (playerMaxTime > debugMaxSurvivalTime)
                        debugMaxSurvivalTime = playerMaxTime;
                }
            }
        }

        UpdatePlayerKillDebugInfo();
    }

    // UpdateSurvivalDebugInfo() 함수는 제거

    private void UpdatePlayerKillDebugInfo()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        var players = PhotonNetwork.PlayerList;
        debugPlayerKills = new PlayerKillDebugInfo[players.Length];

        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            int killCount = playerKillCounts.TryGetValue(player.ActorNumber, out int count) ? count : 0;

            debugPlayerKills[i] = new PlayerKillDebugInfo
            {
                playerName = player.NickName,
                playerID = player.ActorNumber,
                killCount = killCount,
                masterStatus = player.IsMasterClient ? "Master" : "Client",
                localStatus = player.IsLocal ? "Local" : "Remote"
            };
        }
    }
    #endregion

    #region 씬 전환 시 정리
    public void CleanupAllResources()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CleanupAllActiveObjects();
        }

        ClearAllData();
    }

    private void CleanupAllActiveObjects()
    {
        // 태우리 정리
        foreach (var kvp in networkTaewooriDict.ToArray())
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(false);
                ReturnTaewooriToPool(kvp.Value);
            }
        }

        // 스몰태우리 정리
        foreach (var kvp in networkSmallTaewooriDict.ToArray())
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(false);
                ReturnSmallTaewooriToPool(kvp.Value);
            }
        }

        // 발사체 정리
        var allParticles = FindObjectsOfType<FireParticles>();
        foreach (var particle in allParticles)
        {
            if (particle.gameObject.activeInHierarchy)
            {
                ReturnFireParticleToPool(particle.gameObject);
            }
        }
    }

    private void ClearAllData()
    {
        networkTaewooriDict.Clear();
        networkSmallTaewooriDict.Clear();
        smallTaewooriCountByTaewoori.Clear();
        respawnQueue.Clear();
        playerKillCounts.Clear();

        // 생존시간 데이터도 정리
        taewooriSpawnTimes.Clear();
        playerTaewooriSurvivalTimes.Clear();

        nextTaewooriID = 0;
        nextSmallTaewooriID = 0;
    }

    public static void PrepareForSceneTransition()
    {
        if (Instance != null)
        {
            Instance.CleanupAllResources();
        }
    }
    #endregion
}
