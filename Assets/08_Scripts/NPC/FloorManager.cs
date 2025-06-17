using System.Collections;
using UnityEngine;

/// <summary>
/// 층별 타입 정의
/// </summary>
public enum FloorEventType
{
    Normal,            // 일반 (아무것도 안함)
    TaewooliWithFire,  // 태우리 + 불 (4,2층)
    SmokeOnly,         // 연기만 (3층)
    FireOnly,          // 불만 (1층)
    SafeArea          // 안전 구역
}

/// <summary>
/// 층 전체를 관리하는 매니저 - 부모 오브젝트에 붙임
/// 모든 스폰을 중앙에서 관리
/// </summary>
public class FloorManager : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("층 기본 설정")]
    [SerializeField] private int floorNumber = 4; // 4,3,2,1층
    [SerializeField] private FloorEventType floorEventType = FloorEventType.Normal;
    [SerializeField] private string playerTag = "Player";

    [Header("웨이포인트 설정 (자식 오브젝트)")]
    [SerializeField] private GameObject startWaypoint; // 시작점 웨이포인트
    [SerializeField] private GameObject endWaypoint;   // 끝점 웨이포인트

    [Header("층별 파티클 그룹 (자식 오브젝트)")]
    [SerializeField] private GameObject allParticleGroup; // 모든 파티클들을 모아둔 빈 오브젝트

    [Header("🔥 통합 스폰 관리")]
    [SerializeField] private float particleStartDelay = 0f; // 파티클 시작 딜레이
    [SerializeField] private float taewooliStartDelay = 10f; // 태우리 생성 시작 딜레이
    [SerializeField] private float taewooliSpawnInterval = 3f; // 태우리 생성 간격

    [Header("다음 층 연결")]
    [SerializeField] private FloorManager nextFloorManager; // 다음 층 매니저
    #endregion

    #region 변수 선언
    private bool startTriggered = false;
    private bool endTriggered = false;
    private bool isActive = false;
    private ExitTaewooliSpawnParticle[] currentTaewooliSpawners;
    private static bool isInitialized = false;
    #endregion

    #region 유니티 라이프사이클
    private void Start()
    {
        // 최초 한 번만 초기화 실행
        if (!isInitialized)
        {
            InitializeAllFloors();
            isInitialized = true;
        }

        // 웨이포인트 트리거 이벤트 연결
        SetupWaypoints();
    }

    /// <summary>
    /// 모든 층 초기화 - 4층만 활성화
    /// </summary>
    private void InitializeAllFloors()
    {
        FloorManager[] allFloors = FindObjectsOfType<FloorManager>(true);

        foreach (var floor in allFloors)
        {
            if (floor.floorNumber == 4)
            {
                floor.ActivateFloor();
                Debug.Log($"4층 활성화: {floor.name}");
            }
            else
            {
                floor.DeactivateFloor();
            }
        }
    }

    /// <summary>
    /// 웨이포인트 트리거 설정
    /// </summary>
    private void SetupWaypoints()
    {
        // 시작점 웨이포인트 트리거 설정
        if (startWaypoint != null)
        {
            WaypointTrigger startTrigger = startWaypoint.GetComponent<WaypointTrigger>();
            if (startTrigger == null)
            {
                startTrigger = startWaypoint.AddComponent<WaypointTrigger>();
            }
            startTrigger.Initialize(this, WaypointType.Start);
        }

        // 끝점 웨이포인트 트리거 설정
        if (endWaypoint != null)
        {
            WaypointTrigger endTrigger = endWaypoint.GetComponent<WaypointTrigger>();
            if (endTrigger == null)
            {
                endTrigger = endWaypoint.AddComponent<WaypointTrigger>();
            }
            endTrigger.Initialize(this, WaypointType.End);
        }
    }
    #endregion

    #region 층 활성화/비활성화
    /// <summary>
    /// 층 활성화
    /// </summary>
    public void ActivateFloor()
    {
        isActive = true;

        // 시작점만 활성화, 끝점은 비활성화
        if (startWaypoint != null)
            startWaypoint.SetActive(true);

        if (endWaypoint != null)
            endWaypoint.SetActive(false);

        // 파티클 그룹은 비활성화 상태로 시작
        if (allParticleGroup != null)
            allParticleGroup.SetActive(false);

        startTriggered = false;
        endTriggered = false;

        Debug.Log($"{floorNumber}층 활성화 완료");
    }

    /// <summary>
    /// 층 비활성화
    /// </summary>
    public void DeactivateFloor()
    {
        isActive = false;

        // 웨이포인트들만 비활성화
        if (startWaypoint != null)
            startWaypoint.SetActive(false);
        if (endWaypoint != null)
            endWaypoint.SetActive(false);

        // 파티클 그룹 비활성화
        if (allParticleGroup != null)
            allParticleGroup.SetActive(false);

        Debug.Log($"{floorNumber}층 비활성화 완료");
    }

    #endregion

    #region 웨이포인트 이벤트 처리
    /// <summary>
    /// 시작점 트리거 (WaypointTrigger에서 호출)
    /// </summary>
    public void OnStartWaypointTriggered()
    {
        if (!isActive || startTriggered)
            return;

        startTriggered = true;
        Debug.Log($"{floorNumber}층 시작 - 중앙집중식 스폰 시작");

        // 층 이벤트 실행 (매니저가 모든 타이밍 관리)
        StartCoroutine(ExecuteFloorEventSequence());

        // 끝점 웨이포인트 활성화
        if (endWaypoint != null)
        {
            endWaypoint.SetActive(true);
            Debug.Log($"{floorNumber}층 끝점 웨이포인트 활성화");
        }
    }

    /// <summary>
    /// 끝점 트리거 (WaypointTrigger에서 호출)
    /// </summary>
    public void OnEndWaypointTriggered()
    {
        if (!isActive || endTriggered)
            return;

        endTriggered = true;
        Debug.Log($"{floorNumber}층 완료");

        // 현재 층 정리
        CleanupCurrentFloor();

        // 다음 층 활성화
        if (nextFloorManager != null)
        {
            nextFloorManager.ActivateFloor();
            Debug.Log($"다음 층 활성화: {nextFloorManager.floorNumber}층");
        }
        else
        {
            Debug.Log($"{floorNumber}층 완료 - 게임 종료 또는 완료 처리");
        }

        // 현재 층 비활성화
        DeactivateFloor();
    }
    #endregion

    #region 🔥 중앙집중식 스폰 시퀀스
    /// <summary>
    /// 층 이벤트 시퀀스 실행 - 매니저가 모든 타이밍 관리
    /// </summary>
    private IEnumerator ExecuteFloorEventSequence()
    {
        Debug.Log($"{floorNumber}층 시퀀스 시작: {floorEventType}");

        // 1단계: 파티클 딜레이 후 활성화
        if (particleStartDelay > 0)
        {
            Debug.Log($"{floorNumber}층 파티클 {particleStartDelay}초 후 시작 예약");
            yield return new WaitForSeconds(particleStartDelay);
        }

        // 파티클 그룹 활성화
        if (allParticleGroup != null)
        {
            allParticleGroup.SetActive(true);
            Debug.Log($"{floorNumber}층 모든 파티클 활성화");

            // 태우리 생성기들 찾기 (아직 활성화하지 않음)
            currentTaewooliSpawners = allParticleGroup.GetComponentsInChildren<ExitTaewooliSpawnParticle>();
            Debug.Log($"{floorNumber}층 태우리 생성기 {currentTaewooliSpawners.Length}개 발견");
        }

        // 2단계: 태우리 시작 딜레이
        if (taewooliStartDelay > 0)
        {
            Debug.Log($"{floorNumber}층 태우리 {taewooliStartDelay}초 후 시작 예약");
            yield return new WaitForSeconds(taewooliStartDelay);
        }

        // 3단계: 태우리 생성기들 순차적으로 활성화
        if (currentTaewooliSpawners != null && currentTaewooliSpawners.Length > 0)
        {
            for (int i = 0; i < currentTaewooliSpawners.Length; i++)
            {
                if (currentTaewooliSpawners[i] != null)
                {
                    // 매니저가 직접 즉시 활성화 (딜레이 없음)
                    currentTaewooliSpawners[i].ActivateImmediately();
                    Debug.Log($"{floorNumber}층 태우리 생성기 {i + 1}/{currentTaewooliSpawners.Length} 즉시 활성화");

                    // 마지막이 아니면 설정된 간격만큼 대기
                    if (i < currentTaewooliSpawners.Length - 1)
                    {
                        yield return new WaitForSeconds(taewooliSpawnInterval);
                    }
                }
            }
            Debug.Log($"{floorNumber}층 모든 태우리 생성기 활성화 완료!");
        }
    }

    /// <summary>
    /// 현재 층 정리
    /// </summary>
    private void CleanupCurrentFloor()
    {
        // 태우리 생성기들 비활성화
        if (currentTaewooliSpawners != null)
        {
            foreach (var spawner in currentTaewooliSpawners)
            {
                if (spawner != null)
                {
                    spawner.SetActive(false);
                }
            }
            Debug.Log($"{floorNumber}층 태우리 생성기 정리 완료");
        }
    }
    #endregion

    #region 퍼블릭 메서드
    /// <summary>
    /// 강제 활성화 (테스트용)
    /// </summary>
    [ContextMenu("층 강제 활성화")]
    public void ForceActivate()
    {
        ActivateFloor();
    }

    /// <summary>
    /// 강제 비활성화 (테스트용)
    /// </summary>
    [ContextMenu("층 강제 비활성화")]
    public void ForceDeactivate()
    {
        DeactivateFloor();
    }

    /// <summary>
    /// 시퀀스 강제 시작 (테스트용)
    /// </summary>
    [ContextMenu("시퀀스 강제 시작")]
    public void ForceStartSequence()
    {
        if (isActive)
        {
            StartCoroutine(ExecuteFloorEventSequence());
        }
    }
    #endregion
}

/// <summary>
/// 웨이포인트 타입
/// </summary>
public enum WaypointType
{
    Start,
    End
}

/// <summary>
/// 웨이포인트 트리거 컴포넌트 - 웨이포인트 오브젝트에 자동으로 추가됨
/// </summary>
public class WaypointTrigger : MonoBehaviour
{
    private FloorManager floorManager;
    private WaypointType waypointType;
    private bool hasTriggered = false;

    public void Initialize(FloorManager manager, WaypointType type)
    {
        floorManager = manager;
        waypointType = type;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || hasTriggered)
            return;

        hasTriggered = true;

        switch (waypointType)
        {
            case WaypointType.Start:
                floorManager.OnStartWaypointTriggered();
                break;
            case WaypointType.End:
                floorManager.OnEndWaypointTriggered();
                break;
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
