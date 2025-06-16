using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 새로운 불 위협 매니저 - 스폰 위치에서 생성 후 플레이어 주변 고정 위치로 이동
/// </summary>
public class FireThreatManager : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("기본 설정")]
    [SerializeField] private GameObject exitTaewooriPrefab;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private string cameraTag = "MainCamera";

    [Header("스폰 설정")]
    [SerializeField] private Transform[] spawnPoints; // 초기 생성 위치들 (기존 방식)
    [SerializeField] private Transform[] threatPositions; // 플레이어 주변 고정 위치들 (4개)
    [SerializeField] private float spawnCooldown = 10f; // 10초 쿨타임
    [SerializeField] private bool enableSpawning = true; // 스폰 활성화
    [SerializeField] private int maxThreats = 4; // 최대 위협 개수

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 1f; // 플레이어 향해 이동하는 속도
    [SerializeField] private float rotationSpeed = 2f; // 회전 속도
    #endregion

    #region 변수 선언
    private List<ExitTaewoori> activeThreats = new List<ExitTaewoori>();
    private int currentSpawnIndex = 0; // 현재 스폰할 포인트 인덱스
    private float spawnTimer = 0f;
    private Coroutine spawnCoroutine;
    #endregion

    #region 프로퍼티
    public int ActiveThreatCount => activeThreats.Count;
    public bool SpawningEnabled => enableSpawning;
    public Camera PlayerCamera => playerCamera;
    #endregion

    #region 유니티 라이프사이클
    private void Start()
    {
        FindPlayerCamera();

        if (enableSpawning)
        {
            StartSpawning();
        }
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
        Debug.Log("플레이어 카메라 찾기 시작...");

        if (playerCamera == null)
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag(cameraTag);
            if (cameraObj != null)
            {
                playerCamera = cameraObj.GetComponent<Camera>();
                if (playerCamera != null)
                {
                    Debug.Log($"플레이어 카메라 발견: {playerCamera.name}");
                }
                else
                {
                    Debug.LogWarning($"'{cameraTag}' 태그 오브젝트에서 Camera 컴포넌트를 찾을 수 없습니다!");
                }
            }
            else
            {
                Debug.LogWarning($"'{cameraTag}' 태그를 가진 카메라를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.Log($"이미 설정된 카메라 사용: {playerCamera.name}");
        }
    }
    #endregion

    #region 스폰 시스템
    /// <summary>
    /// 스폰 시작
    /// </summary>
    public void StartSpawning()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("스폰 포인트가 설정되지 않았습니다!");
            return;
        }

        if (threatPositions == null || threatPositions.Length == 0)
        {
            Debug.LogError("위협 위치들이 설정되지 않았습니다!");
            return;
        }

        if (exitTaewooriPrefab == null)
        {
            Debug.LogError("ExitTaewoori 프리팹이 할당되지 않았습니다!");
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError("플레이어 카메라를 찾을 수 없습니다!");
            return;
        }

        StopSpawning(); // 기존 코루틴 정리
        spawnCoroutine = StartCoroutine(SpawnRoutine());
        Debug.Log("스폰 시스템 시작");
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
    }

    /// <summary>
    /// 스폰 루틴 - 10초마다 스폰 위치에서 생성 후 빈 고정 위치로 이동
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
            else if (activeThreats.Count >= maxThreats)
            {
                Debug.Log($"최대 위협 개수({maxThreats})에 도달. 더 이상 생성하지 않음.");
            }
        }
    }

    /// <summary>
    /// 스폰 위치에서 생성 후 빈 고정 위치로 이동 지시
    /// </summary>
    private void SpawnThreatAndAssignPosition()
    {
        // 1. 빈 고정 위치 찾기
        Transform emptyPosition = FindEmptyThreatPosition();
        if (emptyPosition == null)
        {
            Debug.Log("모든 위협 위치가 점유됨");
            return;
        }

        // 2. 스폰 위치에서 생성
        Transform spawnPoint = GetNextSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("유효한 스폰 포인트를 찾을 수 없습니다!");
            return;
        }

        // 3. 스폰 위치에서 위협 생성
        GameObject threatObj = Instantiate(exitTaewooriPrefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log($"스폰 위치에서 생성: {threatObj.transform.position}");

        ExitTaewoori threat = threatObj.GetComponent<ExitTaewoori>();

        if (threat != null)
        {
            // 4. 고정 위치로 이동하도록 초기화
            threat.Initialize(this, emptyPosition, moveSpeed, rotationSpeed);
            activeThreats.Add(threat);

            Debug.Log($"위협 생성: {spawnPoint.name}에서 생성 → {emptyPosition.name}으로 이동 (총 {activeThreats.Count}/{maxThreats}개)");
        }
        else
        {
            Debug.LogError("ExitTaewoori 컴포넌트를 찾을 수 없습니다!");
            Destroy(threatObj);
        }
    }

    /// <summary>
    /// 다음 스폰 포인트 가져오기 (기존 방식)
    /// </summary>
    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        Transform spawnPoint = null;
        int attempts = 0;

        while (spawnPoint == null && attempts < spawnPoints.Length)
        {
            if (spawnPoints[currentSpawnIndex] != null)
            {
                spawnPoint = spawnPoints[currentSpawnIndex];
            }

            currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Length;
            attempts++;
        }

        return spawnPoint;
    }

    /// <summary>
    /// 비어있는 위협 위치 찾기
    /// </summary>
    private Transform FindEmptyThreatPosition()
    {
        foreach (Transform position in threatPositions)
        {
            if (position == null)
                continue;

            // 해당 위치 근처에 ExitTaewoori가 있는지 확인
            bool isOccupied = false;
            foreach (var threat in activeThreats)
            {
                if (threat != null && Vector3.Distance(threat.transform.position, position.position) < 1f)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
            {
                return position;
            }
        }

        return null; // 모든 위치가 점유됨
    }
    #endregion

    #region 퍼블릭 메서드
    /// <summary>
    /// 스폰 활성화/비활성화
    /// </summary>
    public void SetSpawningEnabled(bool enable)
    {
        enableSpawning = enable;

        if (enable)
        {
            StartSpawning();
        }
        else
        {
            StopSpawning();
        }

        Debug.Log($"스폰 시스템 {(enable ? "활성화" : "비활성화")}");
    }

    /// <summary>
    /// 스폰 쿨타임 설정
    /// </summary>
    public void SetSpawnCooldown(float cooldown)
    {
        spawnCooldown = Mathf.Max(1f, cooldown); // 최소 1초
        Debug.Log($"스폰 쿨타임 변경: {spawnCooldown}초");
    }

    /// <summary>
    /// 이동 속도 설정
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);

        // 기존 위협들에게도 적용
        foreach (var threat in activeThreats)
        {
            if (threat != null)
            {
                threat.SetMoveSpeed(moveSpeed);
            }
        }

        Debug.Log($"이동 속도 변경: {moveSpeed}");
    }

    /// <summary>
    /// 플레이어 카메라 수동 설정
    /// </summary>
    public void SetPlayerCamera(Camera camera)
    {
        playerCamera = camera;

        Debug.Log($"플레이어 카메라 변경: {(camera != null ? camera.name : "없음")}");
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
        Debug.Log("모든 위협 제거 완료");
    }

    /// <summary>
    /// 위협이 제거될 때 호출 (ExitTaewoori에서 호출)
    /// </summary>
    public void OnThreatDestroyed(ExitTaewoori threat)
    {
        if (activeThreats.Contains(threat))
        {
            activeThreats.Remove(threat);
            Debug.Log($"위협 제거됨. 남은 개수: {activeThreats.Count}");
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
    }
    #endregion

    #region 디버그
    private void OnDrawGizmosSelected()
    {
        // 플레이어 카메라 위치 표시
        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerCamera.transform.position, 2f);
        }

        // 스폰 포인트들 표시
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    // 현재 스폰 예정 포인트는 빨간색, 나머지는 노란색
                    Gizmos.color = (i == currentSpawnIndex) ? Color.red : Color.yellow;
                    Gizmos.DrawWireSphere(spawnPoints[i].position, 1f);
                    Gizmos.DrawRay(spawnPoints[i].position, Vector3.up * 3f);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(spawnPoints[i].position + Vector3.up * 3f,
                        $"Spawn {i}" + (i == currentSpawnIndex ? " (다음)" : ""));
#endif
                }
            }
        }

        // 위협 위치들 표시
        if (threatPositions != null)
        {
            for (int i = 0; i < threatPositions.Length; i++)
            {
                if (threatPositions[i] != null)
                {
                    // 점유된 위치는 빨간색, 빈 위치는 파란색
                    bool isOccupied = false;
                    foreach (var threat in activeThreats)
                    {
                        if (threat != null && Vector3.Distance(threat.transform.position, threatPositions[i].position) < 1f)
                        {
                            isOccupied = true;
                            break;
                        }
                    }

                    Gizmos.color = isOccupied ? Color.red : Color.blue;
                    Gizmos.DrawWireCube(threatPositions[i].position, Vector3.one * 0.5f);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(threatPositions[i].position + Vector3.up * 1f,
                        $"Threat{i}" + (isOccupied ? " (점유)" : " (빈자리)"));
#endif
                }
            }
        }

        // 활성 상태 표시
        if (transform.position != Vector3.zero)
        {
            Gizmos.color = enableSpawning ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 5f, Vector3.one * 2f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 5f,
                enableSpawning ? $"SPAWNING ON\n{activeThreats.Count}개 활성" : "SPAWNING OFF");
#endif
        }
    }
    #endregion
}
