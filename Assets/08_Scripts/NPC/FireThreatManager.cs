using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 단일 불 위협 매니저 - 스폰 관리만 담당
/// </summary>
public class FireThreatManager : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("기본 설정")]
    [SerializeField] private GameObject exitTaewooriPrefab;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private string cameraTag = "MainCamera";

    [Header("층별 태우리 생성위치")]
    [SerializeField] private Transform[] floor4SpawnPoints; // 4층 스폰 포인트들
    [SerializeField] private Transform[] floor2SpawnPoints; // 2층 스폰 포인트들

    [Header("카메라앞 태우리 위치")]
    [SerializeField] private Transform[] exitTaewoorPositions; // 플레이어 주변 고정 위치들
    [SerializeField] private float spawnCooldown = 10f; // 10초 쿨타임
    [SerializeField] private bool enableSpawning = false; // 기본값을 false로 변경
    [SerializeField] private int maxThreats = 4; // 최대 위협 개수
    #endregion

    #region 변수 선언
    private List<ExitTaewoori> activeThreats = new List<ExitTaewoori>();
    private bool[] positionOccupied; // 각 위치의 점유 상태
    private int currentFloor = 0; // 현재 활성화된 층 (0=비활성화, 4=4층, 2=2층)
    private int currentSpawnIndex = 0; // 현재 스폰할 포인트 인덱스
    private Coroutine spawnCoroutine;
    #endregion

    #region 프로퍼티
    public int ActiveThreatCount => activeThreats.Count;
    public bool SpawningEnabled => enableSpawning;
    public Camera PlayerCamera => playerCamera;
    public int CurrentFloor => currentFloor;
    #endregion

    #region 유니티 라이프사이클
    private void Start()
    {
        FindPlayerCamera();

        // 위치 점유 배열 초기화
        if (exitTaewoorPositions != null)
        {
            positionOccupied = new bool[exitTaewoorPositions.Length];
        }

        Debug.Log("FireThreatManager 초기화 완료");
    }

    private void OnDestroy()
    {
        StopSpawning();
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 플레이어 카메라 찾기
    /// </summary>
    private void FindPlayerCamera()
    {
        if (playerCamera == null)
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag(cameraTag);
            if (cameraObj != null)
            {
                playerCamera = cameraObj.GetComponent<Camera>();
                Debug.Log($"카메라 찾기 완료: {playerCamera.name}");
            }
            else
            {
                Debug.LogWarning($"카메라 태그 '{cameraTag}' 찾을 수 없음");
            }
        }
    }
    #endregion

    #region 스폰 시스템
    /// <summary>
    /// 특정 층의 스폰 시작
    /// </summary>
    public void StartSpawningForFloor(int floorNumber)
    {
        if (floorNumber != 4 && floorNumber != 2)
        {
            Debug.LogWarning($"지원하지 않는 층수: {floorNumber} (4층, 2층만 지원)");
            return;
        }

        StopSpawning(); // 기존 스폰 중지
        currentFloor = floorNumber;
        enableSpawning = true;
        currentSpawnIndex = 0; // 스폰 인덱스 초기화
        spawnCoroutine = StartCoroutine(SpawnRoutine());
        Debug.Log($"{floorNumber}층 태우리 스폰 시작");
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
        enableSpawning = false;
        currentFloor = 0;
        Debug.Log("태우리 스폰 중지");
    }

    /// <summary>
    /// 스폰 루틴 - 현재 층의 스폰 포인트에서만 생성
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        while (enableSpawning)
        {
            yield return new WaitForSeconds(spawnCooldown);

            if (enableSpawning && activeThreats.Count < maxThreats)
            {
                SpawnThreatAndAssignPosition();
            }
        }
    }

    /// <summary>
    /// 현재 층의 스폰 위치에서 생성 후 빈 고정 위치로 이동 지시
    /// </summary>
    private void SpawnThreatAndAssignPosition()
    {
        // 1. 빈 고정 위치 찾기
        int emptyPositionIndex = FindEmptyPositionIndex();
        if (emptyPositionIndex == -1)
        {
            Debug.Log($"{currentFloor}층 빈 위치 없음 - 스폰 취소");
            return;
        }

        Transform emptyPosition = exitTaewoorPositions[emptyPositionIndex];

        // 2. 현재 층의 스폰 위치에서 생성
        Transform spawnPoint = GetCurrentFloorSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.Log($"{currentFloor}층 스폰 포인트 없음");
            return;
        }

        // 3. 스폰 위치에서 위협 생성
        GameObject threatObj = Instantiate(exitTaewooriPrefab, spawnPoint.position, spawnPoint.rotation);

        ExitTaewoori threat = threatObj.GetComponent<ExitTaewoori>();

        if (threat != null)
        {
            // 4. 위치 점유 표시
            positionOccupied[emptyPositionIndex] = true;

            // 5. 고정 위치로 이동하도록 초기화
            threat.Initialize(this, emptyPosition);
            activeThreats.Add(threat);

            Debug.Log($"{currentFloor}층 태우리 생성 완료 - 위치: {emptyPositionIndex}번 ({emptyPosition.name}) - 총 {activeThreats.Count}개");
        }
        else
        {
            Destroy(threatObj);
            Debug.LogError($"{currentFloor}층 ExitTaewoori 컴포넌트 없음");
        }
    }

    /// <summary>
    /// 현재 층의 스폰 포인트 가져오기
    /// </summary>
    private Transform GetCurrentFloorSpawnPoint()
    {
        Transform[] currentSpawnPoints = GetCurrentFloorSpawnPoints();

        if (currentSpawnPoints == null || currentSpawnPoints.Length == 0)
        {
            Debug.LogWarning($"{currentFloor}층 스폰 포인트 배열이 비어있음");
            return null;
        }

        Transform spawnPoint = null;
        int attempts = 0;

        while (spawnPoint == null && attempts < currentSpawnPoints.Length)
        {
            if (currentSpawnPoints[currentSpawnIndex] != null)
            {
                spawnPoint = currentSpawnPoints[currentSpawnIndex];
            }

            currentSpawnIndex = (currentSpawnIndex + 1) % currentSpawnPoints.Length;
            attempts++;
        }

        return spawnPoint;
    }

    /// <summary>
    /// 현재 층의 스폰 포인트 배열 반환
    /// </summary>
    private Transform[] GetCurrentFloorSpawnPoints()
    {
        switch (currentFloor)
        {
            case 4:
                return floor4SpawnPoints;
            case 2:
                return floor2SpawnPoints;
            default:
                return null;
        }
    }

    /// <summary>
    /// 빈 위치의 인덱스 찾기 (순차적으로)
    /// </summary>
    private int FindEmptyPositionIndex()
    {
        if (exitTaewoorPositions == null || exitTaewoorPositions.Length == 0)
        {
            Debug.LogWarning("exitTaewoorPositions 배열이 비어있음");
            return -1;
        }

        if (positionOccupied == null || positionOccupied.Length != exitTaewoorPositions.Length)
        {
            Debug.LogWarning("positionOccupied 배열 크기 불일치");
            positionOccupied = new bool[exitTaewoorPositions.Length];
        }

        for (int i = 0; i < exitTaewoorPositions.Length; i++)
        {
            if (exitTaewoorPositions[i] == null)
            {
                Debug.LogWarning($"exitTaewoorPositions[{i}]가 null입니다");
                continue;
            }

            if (!positionOccupied[i])
            {
                Debug.Log($"빈 위치 찾음: {i}번 ({exitTaewoorPositions[i].name})");
                return i;
            }
            else
            {
                Debug.Log($"위치 {i}번 이미 점유됨");
            }
        }

        Debug.LogWarning("모든 위치가 점유됨");
        return -1;
    }
    #endregion

    #region 퍼블릭 메서드
    /// <summary>
    /// 스폰 활성화/비활성화 (기존 호환성용)
    /// </summary>
    public void SetSpawningEnabled(bool enable)
    {
        if (!enable)
        {
            StopSpawning();
        }
        // enable=true인 경우는 StartSpawningForFloor()를 직접 호출해야 함
    }

    /// <summary>
    /// 스폰 쿨타임 설정
    /// </summary>
    public void SetSpawnCooldown(float cooldown)
    {
        spawnCooldown = Mathf.Max(1f, cooldown); // 최소 1초
    }

    /// <summary>
    /// 모든 위협 제거
    /// </summary>
    public void ClearAllThreats()
    {
        foreach (var threat in activeThreats)
        {
            if (threat != null)
            {
                Destroy(threat.gameObject);
            }
        }

        activeThreats.Clear();

        // 위치 점유 상태 초기화
        if (positionOccupied != null)
        {
            for (int i = 0; i < positionOccupied.Length; i++)
            {
                positionOccupied[i] = false;
            }
        }

        Debug.Log("모든 태우리 제거 완료");
    }

    /// <summary>
    /// 위협이 제거될 때 호출 (ExitTaewoori에서 호출)
    /// </summary>
    public void OnThreatDestroyed(ExitTaewoori threat)
    {
        if (activeThreats.Contains(threat))
        {
            activeThreats.Remove(threat);

            // 해당 태우리가 점유하고 있던 위치 해제
            ReleasePosition(threat);

            Debug.Log($"태우리 제거됨 - 남은 개수: {activeThreats.Count}");
        }
    }

    /// <summary>
    /// 태우리가 점유하고 있던 위치 해제
    /// </summary>
    private void ReleasePosition(ExitTaewoori threat)
    {
        if (exitTaewoorPositions == null || positionOccupied == null)
            return;

        for (int i = 0; i < exitTaewoorPositions.Length; i++)
        {
            if (exitTaewoorPositions[i] != null && positionOccupied[i])
            {
                // 태우리가 해당 위치 근처에 있었는지 확인
                float distance = Vector3.Distance(threat.transform.position, exitTaewoorPositions[i].position);
                if (distance < 3f) // 3m 이내면 해당 위치를 점유하고 있었다고 판단
                {
                    positionOccupied[i] = false;
                    Debug.Log($"위치 {i}번 해제됨");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 즉시 위협 생성 (테스트용)
    /// </summary>
    [ContextMenu("즉시 위협 생성")]
    public void SpawnThreatNow()
    {
        if (enableSpawning)
        {
            SpawnThreatAndAssignPosition();
        }
        else
        {
            Debug.Log("스폰이 비활성화됨");
        }
    }
    #endregion
}
