using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

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
    [SerializeField] private float feverTimeCoolTime = 0.5f;

    [Header("===== 스코어 매니저 연결 =====")]
    [SerializeField] private ScoreManager scoreManager;
    #endregion

    #region 변수 선언
    // 오브젝트 풀
    private Queue<GameObject> taewooriPool = new Queue<GameObject>();
    private Queue<GameObject> smallTaewooriPool = new Queue<GameObject>();
    private Queue<GameObject> fireParticlePool = new Queue<GameObject>();

    // 스몰태우리 개수 관리
    private Dictionary<Taewoori, int> smallTaewooriCountByTaewoori = new Dictionary<Taewoori, int>();

    // 네트워크 동기화용 추적
    private Dictionary<int, GameObject> networkTaewooriDict = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> networkSmallTaewooriDict = new Dictionary<int, GameObject>();
    private int nextTaewooriID = 0;
    private int nextSmallTaewooriID = 0;

    // 점수 시스템
    private SurvivalTracker survivalTracker;

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
    public static TaewooriPoolManager Instance => _instance;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 피버타임 상태 확인
    /// </summary>
    private bool IsFeverTime => GameManager.Instance != null &&
                              GameManager.Instance.CurrentPhase == GamePhase.Fever;

    /// <summary>
    /// 생존시간 추적 중인지 여부
    /// </summary>
    public bool IsSurvivalTracking => survivalTracker?.IsTracking ?? false;
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
    }

    private void OnDestroy()
    {
        Taewoori.OnTaewooriDestroyed -= HandleTaewooriDestroyed;
        CleanupAllResources();
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ProcessRespawnQueue();
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        InitializePools();

        survivalTracker = new SurvivalTracker();

        // ScoreManager 자동 찾기
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }
    }

    /// <summary>
    /// 오브젝트 풀 초기화
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
    /// 풀에서 오브젝트 가져오기
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

    #region 생존시간 점수 시스템
    /// <summary>
    /// Fire 페이즈 시작 시 호출
    /// </summary>
    public void StartSurvivalTracking()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        survivalTracker.StartTracking();
    }

    /// <summary>
    /// 피버타임 종료 시 호출
    /// </summary>
    public void EndSurvivalTracking()
    {
        if (!PhotonNetwork.IsMasterClient || !survivalTracker.IsTracking)
            return;

        var scores = survivalTracker.EndTracking(PhotonNetwork.CurrentRoom.PlayerCount);

        // ScoreManager에 점수 전달
        if (scoreManager != null)
        {
            scoreManager.SetScore(ScoreType.Fire_Time, scores.survivalScore);

            if (PhotonNetwork.LocalPlayer != null)
            {
                int playerID = PhotonNetwork.LocalPlayer.ActorNumber;
                int killScore = scores.GetPlayerKillScore(playerID);
                scoreManager.SetScore(ScoreType.Fire_Count, killScore);
            }
        }
    }

    /// <summary>
    /// 태우리 스폰 시 시간 기록
    /// </summary>
    private void RecordTaewooriSpawnTime(int taewooriID)
    {
        if (PhotonNetwork.IsMasterClient && survivalTracker.IsTracking)
        {
            survivalTracker.RecordSpawnTime(taewooriID);
        }
    }

    /// <summary>
    /// 태우리 사망 시 생존시간 계산 및 처치자 기록
    /// </summary>
    public void UpdateSurvivalTimeAndRecordKill(int taewooriID, int killerPlayerID)
    {
        if (PhotonNetwork.IsMasterClient && survivalTracker.IsTracking)
        {
            survivalTracker.UpdateSurvivalTimeAndRecordKill(taewooriID, killerPlayerID);
        }
    }

    /// <summary>
    /// 게임 리셋 시 호출
    /// </summary>
    public void ResetSurvivalTracking()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            survivalTracker.Reset();
        }
    }
    #endregion

    #region 리스폰 시스템
    /// <summary>
    /// 태우리 파괴 이벤트 처리
    /// </summary>
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

    /// <summary>
    /// 리스폰 큐에 화재 오브젝트 추가
    /// </summary>
    private void QueueForRespawn(FireObjScript fireObj)
    {
        if (!PhotonNetwork.IsMasterClient || fireObj == null || !fireObj.IsBurning)
            return;

        // 중복 체크
        if (respawnQueue.Any(entry => entry.FireObj == fireObj))
            return;

        respawnQueue.Add(new RespawnEntry(fireObj));
    }

    /// <summary>
    /// 리스폰 큐 처리
    /// </summary>
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

    #region 스폰 함수들
    /// <summary>
    /// 태우리 생성 (마스터만)
    /// </summary>
    public GameObject SpawnTaewooriAtPosition(Vector3 position, FireObjScript fireObj)
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

    /// <summary>
    /// 태우리 스폰 가능 여부 확인
    /// </summary>
    private bool CanSpawnTaewoori(FireObjScript fireObj)
    {
        if (fireObj == null || !fireObj.IsBurning || fireObj.HasActiveTaewoori())
            return false;

        var preventable = fireObj.GetComponent<FirePreventable>();
        return preventable == null || !preventable.IsFirePreventable;
    }

    /// <summary>
    /// 태우리 설정
    /// </summary>
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
            RecordTaewooriSpawnTime(taewooriID);
        }

        taewooriObj.SetActive(true);
    }

    /// <summary>
    /// 발사체 생성 (마스터만)
    /// </summary>
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

    /// <summary>
    /// 발사체 설정
    /// </summary>
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

    /// <summary>
    /// 스몰태우리 생성 (마스터만)
    /// </summary>
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

    /// <summary>
    /// 스몰태우리 설정
    /// </summary>
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

        if (networkSmallTaewooriDict.TryGetValue(smallTaewooriID, out GameObject smallTaewooriObj))
        {
            var smallTaewoori = smallTaewooriObj.GetComponent<SmallTaewoori>();
            if (smallTaewoori != null)
            {
                smallTaewoori.DieAsClient();
            }
            networkSmallTaewooriDict.Remove(smallTaewooriID);
        }
    }

    [PunRPC]
    void RequestTaewooriDamage(int taewooriID, float damage, int senderID)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (networkTaewooriDict.TryGetValue(taewooriID, out GameObject taewooriObj))
        {
            var taewoori = taewooriObj.GetComponent<Taewoori>();
            if (taewoori != null && !taewoori.IsDead)
            {
                taewoori.TakeDamage(damage);
            }
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

    #region 씬 전환 시 정리
    /// <summary>
    /// 씬 전환 시 모든 리소스 정리
    /// </summary>
    public void CleanupAllResources()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CleanupAllActiveObjects();
        }

        ClearAllData();
    }

    /// <summary>
    /// 활성화된 모든 오브젝트 정리
    /// </summary>
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

    /// <summary>
    /// 모든 데이터 정리
    /// </summary>
    private void ClearAllData()
    {
        networkTaewooriDict.Clear();
        networkSmallTaewooriDict.Clear();
        smallTaewooriCountByTaewoori.Clear();
        respawnQueue.Clear();

        survivalTracker?.Reset();

        nextTaewooriID = 0;
        nextSmallTaewooriID = 0;
    }

    /// <summary>
    /// 외부에서 씬 전환 시 호출할 메서드
    /// </summary>
    public static void PrepareForSceneTransition()
    {
        if (Instance != null)
        {
            Instance.CleanupAllResources();
        }
    }
    #endregion
}

#region 생존시간 추적 클래스
/// <summary>
/// 생존시간 및 처치 점수를 관리하는 별도 클래스
/// </summary>
public class SurvivalTracker
{
    private bool isTracking = false;
    private float maxSurvivalTime = 0f;
    private Dictionary<int, float> taewooriSpawnTimes = new Dictionary<int, float>();
    private Dictionary<int, int> playerTaewooriKills = new Dictionary<int, int>();
    private Dictionary<int, int> playerKillScores = new Dictionary<int, int>();

    public bool IsTracking => isTracking;

    /// <summary>
    /// 추적 시작
    /// </summary>
    public void StartTracking()
    {
        isTracking = true;
        maxSurvivalTime = 0f;
        taewooriSpawnTimes.Clear();
        playerTaewooriKills.Clear();
        playerKillScores.Clear();
    }

    /// <summary>
    /// 추적 종료 및 최종 점수 계산
    /// </summary>
    public SurvivalScoreResult EndTracking(int playerCount)
    {
        isTracking = false;

        int survivalScore = CalculateSurvivalScore(playerCount, maxSurvivalTime);

        var result = new SurvivalScoreResult
        {
            survivalScore = survivalScore,
            maxSurvivalTime = maxSurvivalTime,
            playerKillScores = new Dictionary<int, int>(playerKillScores)
        };

        return result;
    }

    /// <summary>
    /// 리셋
    /// </summary>
    public void Reset()
    {
        isTracking = false;
        maxSurvivalTime = 0f;
        taewooriSpawnTimes.Clear();
        playerTaewooriKills.Clear();
        playerKillScores.Clear();
    }

    /// <summary>
    /// 태우리 스폰 시간 기록
    /// </summary>
    public void RecordSpawnTime(int taewooriID)
    {
        if (isTracking)
        {
            taewooriSpawnTimes[taewooriID] = Time.time;
        }
    }

    /// <summary>
    /// 생존시간 업데이트 및 처치 기록
    /// </summary>
    public void UpdateSurvivalTimeAndRecordKill(int taewooriID, int killerPlayerID)
    {
        if (!isTracking)
            return;

        // 생존시간 계산
        if (taewooriSpawnTimes.TryGetValue(taewooriID, out float spawnTime))
        {
            float survivalTime = Time.time - spawnTime;
            if (survivalTime > maxSurvivalTime)
            {
                maxSurvivalTime = survivalTime;
            }
            taewooriSpawnTimes.Remove(taewooriID);
        }

        // 처치 기록
        RecordPlayerKill(killerPlayerID);
    }

    /// <summary>
    /// 플레이어 처치 기록
    /// </summary>
    private void RecordPlayerKill(int killerPlayerID)
    {
        if (!playerTaewooriKills.ContainsKey(killerPlayerID))
        {
            playerTaewooriKills[killerPlayerID] = 0;
        }

        playerTaewooriKills[killerPlayerID]++;

        // 처치 점수 계산
        int killCount = playerTaewooriKills[killerPlayerID];
        int killScore = CalculateKillScore(killCount);
        playerKillScores[killerPlayerID] = killScore;
    }

    /// <summary>
    /// 생존시간별 점수 계산
    /// </summary>
    private int CalculateSurvivalScore(int playerCount, float survivalTime)
    {
        var thresholds = GetSurvivalThresholds(playerCount);

        if (survivalTime <= thresholds.maxTime)
            return 25;
        else if (survivalTime <= thresholds.midTime)
            return 20;
        else
            return 15;
    }

    /// <summary>
    /// 인원수별 생존시간 임계값 가져오기
    /// </summary>
    private (float maxTime, float midTime) GetSurvivalThresholds(int playerCount)
    {
        return playerCount switch
        {
            1 => (40f, 60f),
            2 => (30f, 45f),
            3 => (24f, 36f),
            4 => (20f, 30f),
            5 => (17f, 26f),
            6 => (15f, 23f),
            _ => (20f, 30f)
        };
    }

    /// <summary>
    /// 처치 수별 점수 계산
    /// </summary>
    private int CalculateKillScore(int killCount)
    {
        if (killCount >= 30)
            return 25;
        if (killCount >= 24)
            return 20;
        return 15;
    }
}

/// <summary>
/// 생존 점수 결과 클래스
/// </summary>
public class SurvivalScoreResult
{
    public int survivalScore;
    public float maxSurvivalTime;
    public Dictionary<int, int> playerKillScores;

    public int GetPlayerKillScore(int playerID)
    {
        return playerKillScores.TryGetValue(playerID, out int score) ? score : 0;
    }
}
#endregion
