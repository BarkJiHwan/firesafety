using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class TestPoolManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static TestPoolManager Instance;

    // 프리팹 ID 기반 풀 관리를 위한 딕셔너리
    private Dictionary<int, IObjectPool<GameObject>> poolDictionary = new Dictionary<int, IObjectPool<GameObject>>();

    // 풀 설정
    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxPoolSize = 50;
    [SerializeField] private bool collectionChecks = true;  // 디버그용: 이미 풀에 있는 객체를 다시 반환하는지 검사

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 오브젝트 풀 가져오기 (없으면 생성)
    public IObjectPool<GameObject> GetPool(GameObject prefab)
    {
        int prefabId = prefab.GetInstanceID();

        // 이미 존재하는 풀 반환
        if (poolDictionary.TryGetValue(prefabId, out var pool))
        {
            return pool;
        }

        // 새 풀 생성
        pool = new ObjectPool<GameObject>(
            createFunc: () => CreatePooledItem(prefab),
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: collectionChecks,
            defaultCapacity: defaultCapacity,
            maxSize: maxPoolSize
        );

        poolDictionary.Add(prefabId, pool);
        return pool;
    }

    // 풀에서 새 오브젝트 생성
    private GameObject CreatePooledItem(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);

        // 풀 ID 저장용 컴포넌트 추가
        PoolIdentifier identifier = obj.AddComponent<PoolIdentifier>();
        identifier.PrefabId = prefab.GetInstanceID();

        // 풀 매니저를 부모로 설정 (선택 사항, 계층 관리용)
        obj.transform.SetParent(transform);

        return obj;
    }

    // 풀에서 꺼낼 때 호출
    private void OnTakeFromPool(GameObject obj)
    {
        obj.SetActive(true);

        // 리지드바디 초기화
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // 풀로 반환할 때 호출
    private void OnReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
    }

    // 풀에서 제거할 때 호출 (최대 크기 초과 등의 이유)
    private void OnDestroyPoolObject(GameObject obj)
    {
        Destroy(obj);
    }

    // 풀에서 오브젝트 가져오기 (위치, 회전 지정)
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var pool = GetPool(prefab);
        GameObject obj = pool.Get();

        // 위치와 회전 설정
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        return obj;
    }

    // 오브젝트를 풀로 반환
    public void Release(GameObject obj)
    {
        if (obj == null)
            return;

        PoolIdentifier identifier = obj.GetComponent<PoolIdentifier>();
        if (identifier == null)
        {
            Debug.LogWarning("풀에서 생성되지 않은 오브젝트를 반환하려고 시도했습니다: " + obj.name);
            Destroy(obj);
            return;
        }

        if (poolDictionary.TryGetValue(identifier.PrefabId, out var pool))
        {
            pool.Release(obj);
        }
        else
        {
            Debug.LogWarning("해당 ID의 풀을 찾을 수 없습니다: " + identifier.PrefabId);
            Destroy(obj);
        }
    }

    // 오브젝트를 지연 반환 (코루틴 필요 없이 바로 호출 가능)
    public void ReleaseAfterDelay(GameObject obj, float delay)
    {
        if (obj == null)
            return;
        StartCoroutine(ReleaseWithDelay(obj, delay));
    }

    // 지연 반환용 코루틴
    private IEnumerator ReleaseWithDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            Release(obj);
        }
    }

    // 풀 ID 저장용 컴포넌트
    private class PoolIdentifier : MonoBehaviour
    {
        public int PrefabId;
    }
}
