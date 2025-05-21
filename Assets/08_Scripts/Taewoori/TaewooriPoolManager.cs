using System.Collections.Generic;
using UnityEngine;

public class TaewooriPoolManager : MonoBehaviour
{
    [Header("===== 프리팹 설정 =====")]
    [SerializeField] private GameObject taewooriPrefab;
    [SerializeField] private GameObject smallTaewooriPrefab;
    [SerializeField] private GameObject fireParticlePrefab;

    [Header("===== 풀 설정 =====")]
    [SerializeField] private int initialPoolSize = 10;

    [Header("===== 리스폰 설정 =====")]
    [SerializeField] private float baseRespawnTime = 10f;
    [SerializeField] private float feverTimecCoolTime = 0.5f;

    // 오브젝트 풀
    private Queue<GameObject> taewooriPool = new Queue<GameObject>();
    private Queue<GameObject> smallTaewooriPool = new Queue<GameObject>();
    private Queue<GameObject> fireParticlePool = new Queue<GameObject>();

    // 발사체 추적
    private Dictionary<Taewoori, int> projectileCountByTaewoori = new Dictionary<Taewoori, int>();

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
        // 리스폰 큐 처리
        ProcessRespawnQueue();
    }

    // 태우리 파괴 이벤트 핸들러
    private void HandleTaewooriDestroyed(Taewoori taewoori, FireObjScript fireObj)
    {
        if (fireObj != null)
        {
            // 활성 태우리 참조 지우기
            fireObj.ClearActiveTaewoori();

            // 리스폰 큐에 추가
            QueueForRespawn(fireObj);
        }
    }

    // 리스폰 큐에 추가
    private void QueueForRespawn(FireObjScript fireObj)
    {
        if (fireObj == null || !fireObj.IsBurning)
            return;

        // 이미 큐에 있는지 확인
        foreach (var entry in respawnQueue)
        {
            if (entry.FireObj == fireObj)
                return; // 이미 큐에 있으면 무시
        }

        // 새 항목 추가
        respawnQueue.Add(new RespawnEntry(fireObj));
        Debug.Log($"<color=orange>리스폰 큐에 추가됨: {fireObj.name}</color>");
    }

    // 리스폰 큐 처리
    private void ProcessRespawnQueue()
    {
        float respawnTime = IsFeverTime ? baseRespawnTime * feverTimecCoolTime : baseRespawnTime;

        // 안전하게 복사본으로 작업
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
                // 리스폰 시간이 되면 태우리 생성
                SpawnTaewooriAtPosition(entry.FireObj.TaewooriPos(), entry.FireObj);
                completedEntries.Add(entry);
                Debug.Log($"<color=green>리스폰 완료: {entry.FireObj.name}</color>");
            }

        }
        foreach (var entry in completedEntries)
        {
            respawnQueue.Remove(entry);
        }
    }

    private void InitializePools()
    {
        // 초기 풀 생성
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

    // 태우리 생성
    
    public GameObject SpawnTaewooriAtPosition(Vector3 position, FireObjScript fireObj)
    {
        if (fireObj == null || !fireObj.IsBurning)
            return null;

        // 이미 태우리가 있는지 확인
        if (fireObj.HasActiveTaewoori())
        {
            Debug.Log($"{fireObj.name}에 이미 태우리가 있어 생성을 건너뜁니다.");
            return null;
        }

        // 예방된 오브젝트는 태우리 생성 안함
        FirePreventable preventable = fireObj.GetComponent<FirePreventable>();
        if (preventable != null && preventable.IsFirePreventable)
        {
            Debug.Log($"{fireObj.name}은 예방 완료되어 태우리 생성 건너뜁니다.");
            return null;
        }

        GameObject taewooriObj = GetFromPool(taewooriPool, taewooriPrefab);

        if (taewooriObj != null)
        {
            // 위치 설정
            taewooriObj.transform.position = position;

            // 태우리 초기화
            Taewoori taewooriComponent = taewooriObj.GetComponent<Taewoori>();
            if (taewooriComponent != null)
            {
                taewooriComponent.Initialize(this, fireObj);
                projectileCountByTaewoori[taewooriComponent] = 0;
            }

            // 활성화
            taewooriObj.SetActive(true);

            Debug.Log($"<color=green>태우리 생성됨: {taewooriObj.name}, 소스: {fireObj.name}</color>");
            return taewooriObj;
        }

        return null;
    }

    // 발사체 생성
    public GameObject PoolSpawnFireParticle(Vector3 position, Quaternion rotation, Taewoori taewoori)
    {
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

            // 발사체 카운트 증가
            if (projectileCountByTaewoori.ContainsKey(taewoori))
            {
                projectileCountByTaewoori[taewoori]++;
            }
        }

        return particle;
    }

    // 작은 태우리 생성
    public GameObject PoolSpawnSmallTaewoori(Vector3 position, Taewoori originTaewoori)
    {
        GameObject smallTaewoori = GetFromPool(smallTaewooriPool, smallTaewooriPrefab);

        if (smallTaewoori != null)
        {
            smallTaewoori.transform.position = position;
            smallTaewoori.SetActive(true);

            SmallTaewoori smallTaewooriComponent = smallTaewoori.GetComponent<SmallTaewoori>();
            if (smallTaewooriComponent != null)
            {
                smallTaewooriComponent.Initialize(this, originTaewoori);
            }
        }

        return smallTaewoori;
    }
   
    // 풀에서 오브젝트 가져오기
    private GameObject GetFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        if (pool.Count == 0)
        {
            return CreatePooledObject(prefab, pool);
        }

        GameObject obj = pool.Dequeue();

        // 오브젝트가 파괴된 경우 새로 생성
        if (obj == null)
        {
            return CreatePooledObject(prefab, pool);
        }

        return obj;
    }

    // 태우리 풀로 반환
    public void ReturnTaewooriToPool(GameObject taewooriObj)
    {
        if (taewooriObj == null)
            return;

        // 태우리 컴포넌트
        Taewoori taewoori = taewooriObj.GetComponent<Taewoori>();
        if (taewoori != null && projectileCountByTaewoori.ContainsKey(taewoori))
        {
            projectileCountByTaewoori.Remove(taewoori);
        }

        // 풀로 반환
        taewooriObj.SetActive(false);
        taewooriObj.transform.SetParent(transform);
        taewooriPool.Enqueue(taewooriObj);
    }

    // 발사체 풀로 반환
    public void ReturnFireParticleToPool(GameObject particleObj)
    {
        if (particleObj != null)
        {
            particleObj.SetActive(false);
            particleObj.transform.SetParent(transform);
            fireParticlePool.Enqueue(particleObj);
        }
    }

    // 작은 태우리 풀로 반환
    public void ReturnSmallTaewooriToPool(GameObject smallTaewooriObj)
    {
        if (smallTaewooriObj == null)
            return;

        // 작은 태우리 컴포넌트 가져오기
        SmallTaewoori smallTaewoori = smallTaewooriObj.GetComponent<SmallTaewoori>();
        if (smallTaewoori != null && smallTaewoori.OriginTaewoori != null)
        {
            // 원본 태우리의 발사체 카운트 감소
            Taewoori originTaewoori = smallTaewoori.OriginTaewoori;
            if (projectileCountByTaewoori.ContainsKey(originTaewoori))
            {
                if (projectileCountByTaewoori[originTaewoori] > 0)
                {
                    projectileCountByTaewoori[originTaewoori]--;
                }
            }
        }

        // 풀로 반환
        smallTaewooriObj.SetActive(false);
        smallTaewooriObj.transform.SetParent(transform);
        smallTaewooriPool.Enqueue(smallTaewooriObj);
    }

    // 태우리가 발사할 수 있는 프로젝타일 수 확인
    public bool CanLaunchProjectile(Taewoori taewoori, int maxProjectiles)
    {
        if (projectileCountByTaewoori.TryGetValue(taewoori, out int count))
        {
            return count < maxProjectiles;
        }
        return false;
    }

    // 피버타임 확인
    private bool IsFeverTime => GameManager.Instance != null &&
                              GameManager.Instance.CurrentPhase == GameManager.GamePhase.Fever;
}
