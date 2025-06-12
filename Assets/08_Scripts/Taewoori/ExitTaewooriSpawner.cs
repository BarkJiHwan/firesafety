using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VR 화재 대피 게임용 불 캐릭터 스포너
/// 오브젝트 풀링을 사용하여 ExitTaewoori를 효율적으로 관리
/// </summary>
public class ExitTaewooriSpawner : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("스폰 설정")]
    [SerializeField] private GameObject exitTaewooriPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int maxActiveCharacters = 8;
    [SerializeField] private int poolSize = 15;

    [Header("스폰 타이밍")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private float minSpawnInterval = 1f;
    [SerializeField] private float maxSpawnInterval = 5f;
    [SerializeField] private bool useRandomInterval = true;
    [SerializeField] private bool autoSpawn = true;

    [Header("웨이포인트 설정")]
    [SerializeField] private bool useWaypoints = true;
    [SerializeField] private Transform[] globalWaypoints; // 모든 캐릭터가 공유할 웨이포인트
    [SerializeField] private Transform[][] spawnPointWaypoints; // 스폰 포인트별 개별 웨이포인트


    #endregion

    #region 변수 선언
    // 오브젝트 풀
    private Queue<GameObject> taewooriPool = new Queue<GameObject>();
    private List<ExitTaewoori> activeCharacters = new List<ExitTaewoori>();

    // 스폰 관리
    private Coroutine spawnCoroutine;
    private int currentSpawnIndex = 0;

    // 통계
    private int totalSpawned = 0;
    private int totalDestroyed = 0;

    // 이벤트
    public System.Action<ExitTaewoori> OnTaewooriSpawned;
    public System.Action<ExitTaewoori> OnTaewooriKilled;
    public System.Action<int> OnActiveCountChanged;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 활성화된 캐릭터 수
    /// </summary>
    public int ActiveCharacterCount => activeCharacters.Count;

    /// <summary>
    /// 총 스폰된 캐릭터 수
    /// </summary>
    public int TotalSpawned => totalSpawned;

    /// <summary>
    /// 총 처치된 캐릭터 수
    /// </summary>
    public int TotalDestroyed => totalDestroyed;

    /// <summary>
    /// 현재 최대 캐릭터 수
    /// </summary>
    public int CurrentMaxCharacters => maxActiveCharacters;
    #endregion

    #region 유니티 라이프사이클
    private void Awake()
    {
        InitializePool();
        InitializeSettings();
    }

    private void Start()
    {
        if (autoSpawn)
        {
            StartSpawning();
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 오브젝트 풀 초기화
    /// </summary>
    private void InitializePool()
    {
        if (exitTaewooriPrefab == null)
        {
            Debug.LogError("ExitTaewoori 프리팹이 할당되지 않았습니다!");
            return;
        }

        // 풀에 오브젝트 미리 생성
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(exitTaewooriPrefab, transform);
            obj.SetActive(false);
            taewooriPool.Enqueue(obj);
        }

        Debug.Log($"ExitTaewoori 풀 초기화 완료: {poolSize}개");
    }

    /// <summary>
    /// 설정 초기화
    /// </summary>
    private void InitializeSettings()
    {
        // 스폰 포인트 검증
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("스폰 포인트가 설정되지 않았습니다!");
        }
    }
    #endregion

    #region 스폰 시스템
    /// <summary>
    /// 스폰 시작
    /// </summary>
    public void StartSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        spawnCoroutine = StartCoroutine(SpawnRoutine());
        Debug.Log("ExitTaewoori 스폰 시작");
    }

    /// <summary>
    /// 스폰 중지
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        Debug.Log("ExitTaewoori 스폰 중지");
    }

    /// <summary>
    /// 스폰 루틴
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // 최대 개수에 도달하지 않았으면 스폰
            if (activeCharacters.Count < maxActiveCharacters)
            {
                SpawnTaewoori();
            }

            // 다음 스폰까지 대기
            float waitTime = useRandomInterval ?
                Random.Range(minSpawnInterval, maxSpawnInterval) :
                spawnInterval;

            yield return new WaitForSeconds(waitTime);
        }
    }

    /// <summary>
    /// 단일 태우리 스폰
    /// </summary>
    public ExitTaewoori SpawnTaewoori()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("스폰 포인트가 없습니다!");
            return null;
        }

        if (activeCharacters.Count >= maxActiveCharacters)
        {
            return null;
        }

        // 풀에서 오브젝트 가져오기
        GameObject taewooriObj = GetFromPool();
        if (taewooriObj == null)
        {
            Debug.LogWarning("풀에 사용 가능한 ExitTaewoori가 없습니다!");
            return null;
        }

        // 스폰 위치 설정
        Transform spawnPoint = GetNextSpawnPoint();
        taewooriObj.transform.position = spawnPoint.position;
        taewooriObj.transform.rotation = spawnPoint.rotation;

        // ExitTaewoori 컴포넌트 초기화
        ExitTaewoori exitTaewoori = taewooriObj.GetComponent<ExitTaewoori>();
        if (exitTaewoori != null)
        {
            exitTaewoori.Initialize(this);

            // 웨이포인트 설정
            if (useWaypoints)
            {
                SetWaypointsForTaewoori(exitTaewoori, currentSpawnIndex - 1);
            }

            activeCharacters.Add(exitTaewoori);
        }

        taewooriObj.SetActive(true);
        totalSpawned++;

        // 이벤트 발생
        OnTaewooriSpawned?.Invoke(exitTaewoori);
        OnActiveCountChanged?.Invoke(activeCharacters.Count);

        Debug.Log($"ExitTaewoori 스폰: {activeCharacters.Count}/{maxActiveCharacters}");
        return exitTaewoori;
    }

    /// <summary>
    /// 다음 스폰 포인트 가져오기
    /// </summary>
    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints.Length == 1)
        {
            return spawnPoints[0];
        }

        Transform spawnPoint = spawnPoints[currentSpawnIndex % spawnPoints.Length];
        currentSpawnIndex++;
        return spawnPoint;
    }

    /// <summary>
    /// 태우리에 웨이포인트 설정
    /// </summary>
    private void SetWaypointsForTaewoori(ExitTaewoori taewoori, int spawnPointIndex)
    {
        Transform[] waypoints = null;

        // 스폰 포인트별 개별 웨이포인트가 있으면 사용
        if (spawnPointWaypoints != null &&
            spawnPointIndex < spawnPointWaypoints.Length &&
            spawnPointWaypoints[spawnPointIndex] != null)
        {
            waypoints = spawnPointWaypoints[spawnPointIndex];
        }
        // 없으면 글로벌 웨이포인트 사용
        else if (globalWaypoints != null && globalWaypoints.Length > 0)
        {
            waypoints = globalWaypoints;
        }
    }
    #endregion

    #region 풀 관리
    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    private GameObject GetFromPool()
    {
        if (taewooriPool.Count == 0)
        {
            // 풀이 비어있으면 새로 생성
            if (exitTaewooriPrefab != null)
            {
                GameObject newObj = Instantiate(exitTaewooriPrefab, transform);
                return newObj;
            }
            return null;
        }

        GameObject obj = taewooriPool.Dequeue();
        if (obj == null)
        {
            return GetFromPool(); // 재귀 호출로 다음 오브젝트 시도
        }

        return obj;
    }

    /// <summary>
    /// 풀로 오브젝트 반환
    /// </summary>
    public void ReturnToPool(GameObject taewooriObj)
    {
        if (taewooriObj == null)
            return;

        ExitTaewoori exitTaewoori = taewooriObj.GetComponent<ExitTaewoori>();
        if (exitTaewoori != null && activeCharacters.Contains(exitTaewoori))
        {
            activeCharacters.Remove(exitTaewoori);
            OnActiveCountChanged?.Invoke(activeCharacters.Count);
        }

        taewooriObj.SetActive(false);
        taewooriObj.transform.SetParent(transform);
        taewooriPool.Enqueue(taewooriObj);
    }
    #endregion

    #region 이벤트 처리
    /// <summary>
    /// 태우리 사망 이벤트 처리
    /// </summary>
    public void OnTaewooriDestroyed(ExitTaewoori taewoori)
    {
        if (activeCharacters.Contains(taewoori))
        {
            activeCharacters.Remove(taewoori);
            totalDestroyed++;

            OnTaewooriKilled?.Invoke(taewoori);
            OnActiveCountChanged?.Invoke(activeCharacters.Count);

            Debug.Log($"ExitTaewoori 처치: {totalDestroyed}마리 (활성: {activeCharacters.Count})");
        }
    }
    #endregion

    #region 퍼블릭 메서드
    /// <summary>
    /// 모든 활성 태우리 제거
    /// </summary>
    public void ClearAllActive()
    {
        List<ExitTaewoori> toRemove = new List<ExitTaewoori>(activeCharacters);

        foreach (ExitTaewoori taewoori in toRemove)
        {
            if (taewoori != null)
            {
                taewoori.ForceDestroy();
            }
        }

        activeCharacters.Clear();
        OnActiveCountChanged?.Invoke(0);

        Debug.Log("모든 활성 ExitTaewoori 제거 완료");
    }

    /// <summary>
    /// 수동으로 여러 마리 스폰
    /// </summary>
    public void SpawnMultiple(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (activeCharacters.Count >= maxActiveCharacters)
                break;
            SpawnTaewoori();
        }
    }

    /// <summary>
    /// 설정 업데이트
    /// </summary>
    public void UpdateSettings(int newMaxCharacters, float newSpawnInterval)
    {
        maxActiveCharacters = Mathf.Max(1, newMaxCharacters);
        spawnInterval = Mathf.Max(0.1f, newSpawnInterval);

        Debug.Log($"스포너 설정 업데이트: 최대 {maxActiveCharacters}마리, 간격 {spawnInterval}초");
    }
    #endregion

    #region 디버그
    private void OnDrawGizmosSelected()
    {
        // 스폰 포인트 표시
        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                    Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 2f);
                }
            }
        }

        // 글로벌 웨이포인트 표시
        if (globalWaypoints != null && globalWaypoints.Length > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < globalWaypoints.Length - 1; i++)
            {
                if (globalWaypoints[i] != null && globalWaypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(globalWaypoints[i].position, globalWaypoints[i + 1].position);
                }
            }
        }
    }
    #endregion
}
