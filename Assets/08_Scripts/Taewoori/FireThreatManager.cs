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

    [Header("태우리 생성위치")]
    [SerializeField] private Transform[] exitTaewoorSpawnPoints; // 초기 생성 위치들 (기존 방식)
    [Header("카메라앞 태우리 위치")]
    [SerializeField] private Transform[] exitTaewoorPositions; // 플레이어 주변 고정 위치들 (4개)
    [SerializeField] private float spawnCooldown = 10f; // 10초 쿨타임
    [SerializeField] private bool enableSpawning = true; // 스폰 활성화
    [SerializeField] private int maxThreats = 4; // 최대 위협 개수

    [Header("카메라 따라다니는 속도")]
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

        if (playerCamera == null)
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag(cameraTag);
            if (cameraObj != null)
            {
                playerCamera = cameraObj.GetComponent<Camera>();
            }
        }
    }
    #endregion

    #region 스폰 시스템
    /// <summary>
    /// 스폰 시작
    /// </summary>
    public void StartSpawning()
    {
        StopSpawning(); // 기존 코루틴 정리
        spawnCoroutine = StartCoroutine(SpawnRoutine());
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
            return;
        }

        // 2. 스폰 위치에서 생성
        Transform spawnPoint = GetNextSpawnPoint();
        if (spawnPoint == null)
        {
            return;
        }

        // 3. 스폰 위치에서 위협 생성
        GameObject threatObj = Instantiate(exitTaewooriPrefab, spawnPoint.position, spawnPoint.rotation);

        ExitTaewoori threat = threatObj.GetComponent<ExitTaewoori>();

        if (threat != null)
        {
            // 4. 고정 위치로 이동하도록 초기화
            threat.Initialize(this, emptyPosition, moveSpeed, rotationSpeed);
            activeThreats.Add(threat);
        }
        else
        {
            Destroy(threatObj);
        }
    }

    /// <summary>
    /// 다음 스폰 포인트 가져오기 (기존 방식)
    /// </summary>
    private Transform GetNextSpawnPoint()
    {
        if (exitTaewoorSpawnPoints == null || exitTaewoorSpawnPoints.Length == 0)
            return null;

        Transform spawnPoint = null;
        int attempts = 0;

        while (spawnPoint == null && attempts < exitTaewoorSpawnPoints.Length)
        {
            if (exitTaewoorSpawnPoints[currentSpawnIndex] != null)
            {
                spawnPoint = exitTaewoorSpawnPoints[currentSpawnIndex];
            }

            currentSpawnIndex = (currentSpawnIndex + 1) % exitTaewoorSpawnPoints.Length;
            attempts++;
        }

        return spawnPoint;
    }

    /// <summary>
    /// 비어있는 위협 위치 찾기
    /// </summary>
    private Transform FindEmptyThreatPosition()
    {
        foreach (Transform position in exitTaewoorPositions)
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

    }

    /// <summary>
    /// 스폰 쿨타임 설정
    /// </summary>
    public void SetSpawnCooldown(float cooldown)
    {
        spawnCooldown = Mathf.Max(1f, cooldown); // 최소 1초
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
    }

    /// <summary>
    /// 위협이 제거될 때 호출 (ExitTaewoori에서 호출)
    /// </summary>
    public void OnThreatDestroyed(ExitTaewoori threat)
    {
        if (activeThreats.Contains(threat))
        {
            activeThreats.Remove(threat);
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

}
