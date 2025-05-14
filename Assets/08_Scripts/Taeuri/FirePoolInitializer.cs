using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirePoolInitializer : MonoBehaviour
{
    [System.Serializable]
    public class PoolInfo
    {
        public GameObject prefab;
        public int initialCount = 10;
    }

    [Header("풀 설정")]
    [SerializeField] private List<PoolInfo> poolsToInitialize = new List<PoolInfo>();

    [Header("화재 오브젝트 설정")]
    [SerializeField] private GameObject testPrefab;           // Test 스크립트가 있는 프리팹
    [SerializeField] private int testPrefabCountPerFire = 1;  // 각 화재 오브젝트마다 생성할 Test 프리팹 개수
    [SerializeField] private bool attachToFireObjects = true; // 화재 오브젝트에 자동 연결 여부
    [SerializeField] private float delayBetweenAttach = 0.1f; // 연결 사이의 지연 시간 (선택 사항)

    private void Start()
    {
        StartCoroutine(InitializeProcess());
    }

    private IEnumerator InitializeProcess()
    {
        // 1. 풀 매니저 확인
        if (TestPoolManager.Instance == null)
        {
            Debug.LogWarning("TestPoolManager 씬에 없습니다. 풀링 기능이 동작하지 않습니다.");
            yield break;
        }

        // 2. 일반 풀 초기화
        foreach (PoolInfo poolInfo in poolsToInitialize)
        {
            if (poolInfo.prefab != null)
            {
                PrewarmPool(poolInfo.prefab, poolInfo.initialCount);
                yield return null; // 프레임 드랍 방지를 위한 대기
            }
        }

        // 3. 테스트 프리팹 풀 초기화 (아직 등록되지 않은 경우)
        if (testPrefab != null)
        {
            bool foundInPools = false;
            foreach (PoolInfo poolInfo in poolsToInitialize)
            {
                if (poolInfo.prefab == testPrefab)
                {
                    foundInPools = true;
                    break;
                }
            }

            if (!foundInPools)
            {
                // FireObjMgr에서 필요한 개수 추정
                int estimatedCount = 0;
                if (FireObjMgr.Instance != null)
                {
                    estimatedCount = FireObjMgr.Instance.fireObjects.Count * testPrefabCountPerFire;
                }

                // 추정 개수가 0이면 기본값 사용
                if (estimatedCount <= 0)
                {
                    estimatedCount = 10;
                }

                PrewarmPool(testPrefab, estimatedCount);
                yield return null;
            }
        }

        // 4. FireObjMgr 확인 및 화재 오브젝트에 테스트 프리팹 연결
        if (attachToFireObjects && FireObjMgr.Instance != null && testPrefab != null)
        {
            yield return StartCoroutine(AttachTestPrefabsToFireObjects());
        }
    }

    // 풀 사전 생성 함수
    private void PrewarmPool(GameObject prefab, int count)
    {
        List<GameObject> tempObjects = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = TestPoolManager.Instance.Get(prefab, Vector3.zero, Quaternion.identity);
            tempObjects.Add(obj);
        }

        foreach (GameObject obj in tempObjects)
        {
            TestPoolManager.Instance.Release(obj);
        }

        Debug.Log($"{prefab.name} 프리팹의 풀 {count}개 미리 생성 완료");
    }

    // 화재 오브젝트에 테스트 프리팹 연결 함수
    private IEnumerator AttachTestPrefabsToFireObjects()
    {
        Debug.Log($"화재 오브젝트에 Test 프리팹 연결 시작...");

        List<FireObjScript> fireObjects = FireObjMgr.Instance.fireObjects;
        int count = 0;

        foreach (FireObjScript fireObj in fireObjects)
        {
            if (fireObj == null)
                continue;

            for (int i = 0; i < testPrefabCountPerFire; i++)
            {
                // FirePoolInitializer.cs 내부
                GameObject testObj = TestPoolManager.Instance.Get(testPrefab, fireObj.transform.position, Quaternion.identity);

                // 부모 설정 방법 변경
                testObj.transform.SetParent(fireObj.transform);
                testObj.transform.localPosition = new Vector3(0, 0, 0);
                //인터페이스 함수()

                count++;
            }

            // 프레임 드랍 방지를 위해 약간의 지연
            if (delayBetweenAttach > 0)
            {
                yield return new WaitForSeconds(delayBetweenAttach);
            }
            else
            {
                yield return null;
            }
        }

        Debug.Log($"화재 오브젝트에 총 {count}개의 Test 프리팹 연결 완료");
    }
}
