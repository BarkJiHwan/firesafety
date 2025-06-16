using System.Collections;
using UnityEngine;

/// <summary>
/// 웨이포인트 타입 정의
/// </summary>
public enum WaypointType
{
    FloorStart,         // 층 시작점 (UI + 대기 + 파티클/태우리 자동 실행)
    FloorEnd,           // 층 끝점 (정리 + 다음층 준비)
    BossStart          // 보스 시작점 (다태우리 등장)
}

/// <summary>
/// 층별 타입 정의
/// </summary>
public enum FloorEventType
{
    Normal,            // 일반 (아무것도 안함)
    FireWithEnemies,   // 불 + 태우리 (4,2층)
    SmokeOnly,         // 연기만 (3층)
    FireOnly,          // 불만 (1층 - 태우리 없음)
    SafeArea          // 안전 구역
}

/// <summary>
/// 화재 대피 시뮬레이션용 웨이포인트 시스템 (4층→1층)
/// </summary>
public class FireWaypoint : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("웨이포인트 기본 설정")]
    [SerializeField] private WaypointType waypointType = WaypointType.FloorStart;
    [SerializeField] private int floorNumber = 4; // 4,3,2,1층
    [SerializeField] private string playerTag = "Player";

    [Header("층 이벤트 설정 (FloorStart용)")]
    [SerializeField] private FloorEventType floorEventType = FloorEventType.Normal;
    [SerializeField] private APTController aptController;
    [SerializeField] private FireThreatManager fireThreatManager; // 해당 층 태우리 매니저

    [Header("보스 설정 (BossStart용)")]
    [SerializeField] private DaTaeuri daTaeuriBoss; // 이미 생성된 다태우리 보스

    [Header("시작점 전용 설정")]
    [SerializeField] private GameObject floorEndWaypoint; // 해당 층의 FloorEnd 웨이포인트

    [Header("끝점 전용 설정")]
    [SerializeField] private GameObject nextFloorStartWaypoint; // 다음 층의 FloorStart 웨이포인트
    #endregion

    #region 변수 선언
    private bool hasTriggered = false;
    private bool playerInside = false;
    private static bool isInitialized = false; // 초기화 확인용
    #endregion

    #region 유니티 라이프사이클
    private void Awake()
    {
        // APTController 자동 찾기
        if (aptController == null)
        {
            aptController = GetComponentInParent<APTController>();
            if (aptController == null)
            {
                aptController = FindObjectOfType<APTController>();
            }
        }
    }

    private void Start()
    {
        // 최초 한 번만 초기화 실행
        if (!isInitialized)
        {
            InitializeWaypoints();
            isInitialized = true;
        }
    }

    /// <summary>
    /// 모든 웨이포인트 초기화 - 4층 시작점만 활성화
    /// </summary>
    private void InitializeWaypoints()
    {
        FireWaypoint[] allWaypoints = FindObjectsOfType<FireWaypoint>(true);

        foreach (var waypoint in allWaypoints)
        {
            if (waypoint.waypointType == WaypointType.FloorStart && waypoint.floorNumber == 4)
            {
                waypoint.gameObject.SetActive(true);
                Debug.Log($"4층 시작점 활성화: {waypoint.name}");
            }
            else
            {
                waypoint.gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (hasTriggered)
            return;

        playerInside = true;
        ExecuteWaypointAction();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = false;
    }
    #endregion

    #region 웨이포인트 액션
    /// <summary>
    /// 웨이포인트 타입별 액션 실행
    /// </summary>
    private void ExecuteWaypointAction()
    {
        hasTriggered = true;

        switch (waypointType)
        {
            case WaypointType.FloorStart:
                HandleFloorStart();
                break;

            case WaypointType.FloorEnd:
                HandleFloorEnd();
                break;

            case WaypointType.BossStart:
                HandleBossStart();
                break;
        }
    }

    /// <summary>
    /// 층 시작점 처리 - 즉시 파티클/태우리 실행 + FloorEnd 활성화
    /// </summary>
    private void HandleFloorStart()
    {
        Debug.Log($"{floorNumber}층 시작 - 즉시 이벤트 실행");

        // 해당 층의 이벤트 즉시 실행
        ExecuteFloorEvent();
    }

    /// <summary>
    /// 층 이벤트 실행 - 파티클 및 태우리 생성 + FloorEnd 활성화
    /// </summary>
    private void ExecuteFloorEvent()
    {
        Debug.Log($"{floorNumber}층 이벤트 실행: {floorEventType}");

        switch (floorEventType)
        {
            case FloorEventType.FireWithEnemies:
                // 불 파티클 켜기
                if (aptController != null)
                {
                    aptController.SetFloorFire(floorNumber, true);
                    Debug.Log($"{floorNumber}층 불 파티클 ON");
                }
                else
                {
                    Debug.LogWarning("APTController가 연결되지 않음");
                }

                // 해당 층의 태우리 생성 시작
                if (fireThreatManager != null)
                {
                    fireThreatManager.StartSpawningForFloor(floorNumber);
                    Debug.Log($"{floorNumber}층 태우리 생성 시작");
                }
                else
                {
                    Debug.LogWarning("FireThreatManager가 연결되지 않음");
                }
                break;

            case FloorEventType.SmokeOnly:
                // 연기 파티클만 켜기
                if (aptController != null)
                {
                    aptController.SetFloorFire(floorNumber, true);
                    Debug.Log($"{floorNumber}층 연기 ON");
                }
                break;

            case FloorEventType.FireOnly:
                // 불 파티클만 켜기 (태우리 없음)
                if (aptController != null)
                {
                    aptController.SetFloorFire(floorNumber, true);
                    Debug.Log($"{floorNumber}층 불 파티클 ON (태우리 없음)");
                }
                break;

            case FloorEventType.SafeArea:
                Debug.Log($"{floorNumber}층 안전 구역");
                break;

            case FloorEventType.Normal:
                Debug.Log($"{floorNumber}층 일반 구역 - 이벤트 없음");
                break;
        }

        // 직접 연결된 FloorEnd 웨이포인트 활성화
        ActivateFloorEndWaypoint();
    }

    /// <summary>
    /// 직접 연결된 FloorEnd 웨이포인트 활성화
    /// </summary>
    private void ActivateFloorEndWaypoint()
    {
        if (floorEndWaypoint != null)
        {
            floorEndWaypoint.SetActive(true);
            Debug.Log($"{floorNumber}층 FloorEnd 웨이포인트 활성화: {floorEndWaypoint.name}");
        }
        else
        {
            Debug.LogWarning($"{floorNumber}층 FloorEnd 웨이포인트가 연결되지 않음");
        }
    }

    /// <summary>
    /// 층 끝점 처리 - 현재 층 정리하고 다음 층 시작점 활성화
    /// </summary>
    private void HandleFloorEnd()
    {
        Debug.Log($"{floorNumber}층 완료");

        // 현재 층 정리
        CleanupCurrentFloor();

        // 직접 연결된 다음 층 시작점 활성화
        if (nextFloorStartWaypoint != null)
        {
            nextFloorStartWaypoint.SetActive(true);
            Debug.Log($"다음 층 시작점 활성화: {nextFloorStartWaypoint.name}");
        }
        else
        {
            // 다음 층이 없는 경우 보스 등장 또는 게임 완료
            Debug.LogWarning($"{floorNumber}층 FloorEnd에 다음 층 시작점이 연결되지 않음");
        }
    }

    /// <summary>
    /// 현재 층 정리 (파티클 끄기, 태우리 정리)
    /// </summary>
    private void CleanupCurrentFloor()
    {
        // 파티클 끄기
        if (aptController != null)
        {
            aptController.SetFloorFire(floorNumber, false);
            Debug.Log($"{floorNumber}층 파티클 OFF");
        }

        // 태우리 매니저 정지
        if (fireThreatManager != null)
        {
            fireThreatManager.SetSpawningEnabled(false);
            fireThreatManager.ClearAllThreats();
            Debug.Log($"{floorNumber}층 태우리 정리 완료");
        }
    }

    /// <summary>
    /// 보스 시작점 처리 - 다태우리 보스에게 접근 명령
    /// </summary>
    private void HandleBossStart()
    {
        if (daTaeuriBoss == null)
        {
            Debug.LogWarning("다태우리 보스가 연결되지 않음");
            return;
        }

        // 다태우리에게 플레이어 접근 시작 명령
        daTaeuriBoss.StartApproaching();
        Debug.Log("보스 등장! 플레이어 접근 시작");
    }
    #endregion

    #region 퍼블릭 메서드
    /// <summary>
    /// 웨이포인트 강제 활성화
    /// </summary>
    public void ForceActivate()
    {
        gameObject.SetActive(true);
        hasTriggered = false;
    }
    #endregion

    #region 디버그
    private void OnDrawGizmosSelected()
    {
        Color gizmoColor = GetWaypointColor();
        Gizmos.color = playerInside ? Color.red : gizmoColor;
        Vector3 boxSize = new Vector3(3f, 3f, 3f);
        Gizmos.DrawWireCube(transform.position, boxSize);
    }

    private Color GetWaypointColor()
    {
        switch (waypointType)
        {
            case WaypointType.FloorStart:
                return Color.green;
            case WaypointType.FloorEnd:
                return Color.blue;
            default:
                return Color.gray;
        }
    }
    #endregion
}
