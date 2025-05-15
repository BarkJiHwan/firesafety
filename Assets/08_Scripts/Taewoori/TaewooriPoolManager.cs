using System.Collections.Generic;
using UnityEngine;

public class TaewooriPoolManager : MonoBehaviour
{
    [SerializeField] private GameObject taewooriPrefab;
    [SerializeField] private GameObject smallTaewooriPrefab;
    [SerializeField] private GameObject fireParticlePrefab;

    [SerializeField] private int initialPoolSize = 10;

    // 오브젝트 풀
    private Queue<GameObject> taewooriPool = new Queue<GameObject>();
    private Queue<GameObject> smallTaewooriPool = new Queue<GameObject>();
    private Queue<GameObject> fireParticlePool = new Queue<GameObject>();

    // 활성화된 오브젝트 추적
    private Dictionary<FireObjScript, List<Taewoori>> activeTaewooriesByFireObj = new Dictionary<FireObjScript, List<Taewoori>>();
    private Dictionary<Taewoori, int> projectileCountByTaewoori = new Dictionary<Taewoori, int>();

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

    // FireObjScript에서 호출할 메서드 - 태우리 생성
    public void PoolSpawnTaewooriAt(FireObjScript fireObj)
    {
        if (fireObj == null)
            return;

        // 태우리 스폰 위치 계산 (ITaewooriPos 인터페이스 사용)
        Vector3 spawnPos = fireObj.TaewooriPos();

        // 풀에서 태우리 가져오기
        GameObject taewooriObj = GetFromPool(taewooriPool, taewooriPrefab);

        if (taewooriObj != null)
        {
            taewooriObj.transform.position = spawnPos;
            taewooriObj.SetActive(true);

            Taewoori taewooriComponent = taewooriObj.GetComponent<Taewoori>();
            if (taewooriComponent != null)
            {
                // 사용 중인 태우리 등록
                if (!activeTaewooriesByFireObj.ContainsKey(fireObj))
                {
                    activeTaewooriesByFireObj[fireObj] = new List<Taewoori>();
                }

                activeTaewooriesByFireObj[fireObj].Add(taewooriComponent);
                projectileCountByTaewoori[taewooriComponent] = 0;

                taewooriComponent.Initialize(this, fireObj);
            }
        }
    }

    // 태우리가 발사체를 발사할 때 호출되는 메서드
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

    // 작은 태우리가 파괴될 때 호출되는 메서드
    public void NotifySmallTaewooriDestroyed(Taewoori originTaewoori)
    {
        if (originTaewoori != null && projectileCountByTaewoori.ContainsKey(originTaewoori))
        {
            if (projectileCountByTaewoori[originTaewoori] > 0)
            {
                projectileCountByTaewoori[originTaewoori]--;
            }
        }
    }

    // FireObjScript가 비활성화될 때 관련 태우리 정리
    public void DeactivateTaewoories(FireObjScript fireObj)
    {
        if (fireObj == null || !activeTaewooriesByFireObj.ContainsKey(fireObj))
            return;

        foreach (var taewoori in activeTaewooriesByFireObj[fireObj])
        {
            if (taewoori != null && taewoori.gameObject.activeInHierarchy)
            {
                ReturnToPool(taewoori.gameObject, taewooriPool);

                // 관련 발사체 카운트 제거
                if (projectileCountByTaewoori.ContainsKey(taewoori))
                {
                    projectileCountByTaewoori.Remove(taewoori);
                }
            }
        }

        activeTaewooriesByFireObj[fireObj].Clear();
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

        // 태우리 컴포넌트가 있으면 재설정 처리
        Taewoori taewooriComponent = obj.GetComponent<Taewoori>();
        if (taewooriComponent != null)
        {
            // 이 시점에서는 sourceFireObj를 null로 설정하지 않음
            // 나중에 Initialize에서 새 값으로 설정될 것임
        }

        return obj;
    }

    // 오브젝트 풀로 반환
    public void ReturnToPool(GameObject obj, Queue<GameObject> pool)
    {
        if (obj != null)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            pool.Enqueue(obj);
        }
    }

    // 태우리 반환
    public void ReturnTaewooriToPool(GameObject taewooriObj)
    {
        ReturnToPool(taewooriObj, taewooriPool);

        // 추적 목록에서 제거
        Taewoori taewoori = taewooriObj.GetComponent<Taewoori>();
        if (taewoori != null)
        {
            foreach (var kvp in activeTaewooriesByFireObj)
            {
                kvp.Value.Remove(taewoori);
            }

            if (projectileCountByTaewoori.ContainsKey(taewoori))
            {
                projectileCountByTaewoori.Remove(taewoori);
            }
        }
    }

    // 발사체 반환
    public void ReturnFireParticleToPool(GameObject particleObj)
    {
        ReturnToPool(particleObj, fireParticlePool);
    }

    // 작은 태우리 반환
    public void ReturnSmallTaewooriToPool(GameObject smallTaewooriObj)
    {
        ReturnToPool(smallTaewooriObj, smallTaewooriPool);
    }
    // 특정 위치에 태우리 생성
    // TaewooriPoolManager.cs
    public GameObject SpawnTaewooriAtPosition(Vector3 position, FireObjScript fireObj)
    {
        if (fireObj == null)
        {
            Debug.LogError("[TaewooriPoolManager] SpawnTaewooriAtPosition: fireObj이 null입니다!");
            return null;
        }

        GameObject taewooriObj = GetFromPool(taewooriPool, taewooriPrefab);

        if (taewooriObj != null)
        {
            // 위치 설정
            taewooriObj.transform.position = position;

            Taewoori taewooriComponent = taewooriObj.GetComponent<Taewoori>();
            if (taewooriComponent != null)
            {
                // 중요: 활성화 전에 초기화 수행
                taewooriComponent.Initialize(this, fireObj);

                // 관리 딕셔너리에 추가
                if (!activeTaewooriesByFireObj.ContainsKey(fireObj))
                {
                    activeTaewooriesByFireObj[fireObj] = new List<Taewoori>();
                }

                activeTaewooriesByFireObj[fireObj].Add(taewooriComponent);
                projectileCountByTaewoori[taewooriComponent] = 0;

                // 이제 모든 설정이 완료되었으므로 활성화
                taewooriObj.SetActive(true);

                Debug.Log($"[TaewooriPoolManager] 태우리가 위치 {position}에 생성되고 {fireObj.name}에 연결되었습니다.");
            }
        }

        return taewooriObj;
    }
}
