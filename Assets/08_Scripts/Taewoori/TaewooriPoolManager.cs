using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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
    /// 태우리 생성 (마스터 전용) - 네트워크 ID 할당 후 클라이언트에 동기화
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
            }

            taewooriObj.SetActive(true);

            // 네트워크로 태우리 생성 알림
            photonView.RPC("NetworkSpawnTaewoori", RpcTarget.Others,
                taewooriComponent.NetworkID, spawnPosition, spawnRotation);

            Debug.Log($"[마스터] 태우리 {taewooriComponent.NetworkID} 생성: {spawnPosition}");
            return taewooriObj;
        }

        return null;
    }

    /// <summary>
    /// 발사체 생성 (마스터 전용) - 스몰태우리 생성 제한 체크 후 생성
    /// </summary>
    public GameObject PoolSpawnFireParticle(Vector3 position, Quaternion rotation, Taewoori taewoori)
    {
        if (!PhotonNetwork.IsMasterClient)
            return null;

        int currentCount = GetSmallTaewooriCount(taewoori);
        if (currentCount >= taewoori.MaxSmallTaewooriCount)
        {
            Debug.Log($"파이어 파티클 발사 불가: 스몰 태우리 최대 개수 도달 {currentCount}/{taewoori.MaxSmallTaewooriCount}");
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
    /// 스몰태우리 생성 (마스터 전용) - 네트워크 ID 할당 후 클라이언트에 동기화
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

            Debug.Log($"[마스터] 스몰태우리 {smallTaewooriComponent.NetworkID} 생성");
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
            Debug.Log($"[클라이언트] 태우리 {taewooriID} 시각적 생성 완료");
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

                Debug.Log($"[클라이언트] 스몰태우리 {smallTaewooriID} 시각적 생성 완료");
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
                Debug.Log($"[클라이언트] 태우리 {taewooriID} 체력 동기화: {currentHealth}/{maxHealth}");
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
                Debug.Log($"[클라이언트] 스몰태우리 {smallTaewooriID} 체력 동기화: {currentHealth}/{maxHealth}");
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
            Debug.Log($"[클라이언트] 태우리 {taewooriID} 파괴 동기화");
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
            Debug.Log($"[클라이언트] 스몰태우리 {smallTaewooriID} 파괴 동기화");
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
                Debug.Log($"[마스터] 클라이언트 {senderID}가 태우리 {taewooriID}에게 {damage} 데미지");
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
                Debug.Log($"[마스터] 클라이언트 {senderID}가 스몰태우리 {smallTaewooriID}에게 {damage} 데미지");
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
