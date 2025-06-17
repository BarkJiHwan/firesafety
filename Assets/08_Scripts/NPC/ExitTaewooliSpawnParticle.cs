using UnityEngine;

/// <summary>
/// 불 파티클과 함께 ExitTaewoori를 생성하는 스크립트
/// 빈 오브젝트에 붙이고, 자식으로 불 파티클을 둠
/// 매니저가 모든 스폰을 관리 - 개별 딜레이 제거
/// </summary>
public class ExitTaewooliSpawnParticle : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("태우리 생성 설정")]
    [SerializeField] private GameObject exitTaewooriPrefab;
    [SerializeField] private Camera playerCamera;

    [Header("자동 배정 설정")]
    [SerializeField] private bool autoAssignPosition = true; // 자동으로 위치 배정
    [SerializeField] private string taewooliPositionTag = "TaewooliPosition"; // 태우리 위치 태그
    #endregion

    #region 변수 선언
    private bool isActive = false;
    private bool hasSpawned = false; // 한 번만 생성
    private Transform assignedPosition; // 배정된 위치
    private ExitTaewoori spawnedTaewoori; // 생성된 태우리 참조
    private static Transform[] allTaewooliPositions; // 모든 태우리 위치들
    private static bool[] positionOccupied; // 위치 점유 상태
    private static bool positionsInitialized = false; // 위치 초기화 여부
    private bool managedByFloorManager = true; // 매니저에 의해 관리되는지 여부
    #endregion

    #region 유니티 라이프사이클
    private void Awake()
    {
        // 카메라 자동 찾기
        if (playerCamera == null)
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObj != null)
            {
                playerCamera = cameraObj.GetComponent<Camera>();
            }
        }

        // 태우리 위치들 초기화 (한 번만)
        if (!positionsInitialized)
        {
            InitializeTaewooliPositions();
        }
    }

    private void Start()
    {
        // 매니저에 의해 관리되는 경우 자동 비활성화하지 않음
        if (!managedByFloorManager)
        {
            // 독립적으로 사용되는 경우에만 비활성화
            SetActiveComplete(false);
        }

        Debug.Log($"{gameObject.name} Start() - 매니저 관리: {managedByFloorManager}");
    }

    /// <summary>
    /// 태우리 위치들 초기화 및 자동 배정
    /// </summary>
    private void InitializeTaewooliPositions()
    {
        if (autoAssignPosition)
        {
            // 태그로 모든 태우리 위치 찾기
            GameObject[] positionObjects = GameObject.FindGameObjectsWithTag(taewooliPositionTag);
            allTaewooliPositions = new Transform[positionObjects.Length];

            for (int i = 0; i < positionObjects.Length; i++)
            {
                allTaewooliPositions[i] = positionObjects[i].transform;
            }

            positionOccupied = new bool[allTaewooliPositions.Length];
            positionsInitialized = true;

            Debug.Log($"태우리 위치 {allTaewooliPositions.Length}개 자동 찾기 완료");
        }
    }
    #endregion

    #region  매니저 전용 활성화 메서드
    /// <summary>
    /// 매니저에서 호출 - 즉시 활성화 및 태우리 생성
    /// </summary>
    public void ActivateImmediately()
    {
        Debug.Log($"{gameObject.name} 매니저에서 즉시 활성화 요청");

        // 위치 자동 배정
        if (autoAssignPosition)
        {
            AssignPosition();
        }

        // 전체 오브젝트 활성화 (부모와 자식 모두)
        SetActiveComplete(true);

        // 즉시 태우리 생성
        if (!hasSpawned && assignedPosition != null)
        {
            SpawnTaewoori();
            hasSpawned = true;
        }

        isActive = true;
        Debug.Log($"{gameObject.name} 즉시 활성화 완료");
    }
    #endregion

    #region 활성화/비활성화
    /// <summary>
    /// 파티클 및 태우리 생성 활성화/비활성화
    /// </summary>
    public void SetActive(bool active)
    {
        Debug.Log($"{gameObject.name} SetActive 호출: {active}");

        isActive = active;

        // 전체 오브젝트 활성화/비활성화 (부모와 자식 모두)
        SetActiveComplete(active);

        if (!active)
        {
            // 비활성화 시 위치 해제
            ReleaseAssignedPosition();
        }

        Debug.Log($"{gameObject.name} {(active ? "활성화" : "비활성화")} 완료");
    }

    /// <summary>
    /// 부모 오브젝트와 자식 오브젝트들 모두 활성화/비활성화
    /// </summary>
    private void SetActiveComplete(bool active)
    {
        // 부모 오브젝트 활성화/비활성화
        gameObject.SetActive(active);

        // 부모가 비활성화되면 자식들도 자동으로 비활성화되므로
        // 활성화할 때만 자식들을 명시적으로 활성화
        if (active)
        {
            SetChildrenActive(true);
        }

        Debug.Log($"{gameObject.name} 전체 오브젝트 {(active ? "활성화" : "비활성화")}");
    }

    /// <summary>
    /// 자식 오브젝트들만 활성화/비활성화
    /// </summary>
    private void SetChildrenActive(bool active)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(active);
            Debug.Log($"자식 오브젝트 {i}: {transform.GetChild(i).name} → {active}");
        }
    }

    /// <summary>
    /// 빈 위치 자동 배정
    /// </summary>
    private void AssignPosition()
    {
        if (allTaewooliPositions == null || allTaewooliPositions.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: 태우리 위치가 없습니다");
            return;
        }

        // 빈 위치 찾기
        for (int i = 0; i < allTaewooliPositions.Length; i++)
        {
            if (!positionOccupied[i] && allTaewooliPositions[i] != null)
            {
                positionOccupied[i] = true;
                assignedPosition = allTaewooliPositions[i];

                Debug.Log($"{gameObject.name}: {i}번 위치 배정됨 ({assignedPosition.name})");
                return;
            }
        }

        Debug.LogWarning($"{gameObject.name}: 사용 가능한 위치가 없습니다");
    }

    /// <summary>
    /// 배정된 위치 해제
    /// </summary>
    private void ReleaseAssignedPosition()
    {
        if (assignedPosition == null)
            return;

        // 배정된 위치 찾아서 해제
        for (int i = 0; i < allTaewooliPositions.Length; i++)
        {
            if (allTaewooliPositions[i] == assignedPosition)
            {
                positionOccupied[i] = false;
                Debug.Log($"{gameObject.name}: {i}번 위치 해제됨");
                break;
            }
        }

        assignedPosition = null;
    }
    #endregion

    #region 태우리 생성
    /// <summary>
    /// 태우리 생성
    /// </summary>
    private void SpawnTaewoori()
    {
        if (exitTaewooriPrefab == null)
        {
            Debug.LogWarning($"{gameObject.name}: ExitTaewoori 프리팹이 설정되지 않음");
            return;
        }

        if (assignedPosition == null)
        {
            Debug.LogWarning($"{gameObject.name}: 배정된 위치가 없음");
            return;
        }

        // 현재 위치에서 태우리 생성
        GameObject taewooliObj = Instantiate(exitTaewooriPrefab, transform.position, transform.rotation);
        ExitTaewoori taewoori = taewooliObj.GetComponent<ExitTaewoori>();

        if (taewoori != null)
        {
            // 배정된 위치로 이동하도록 초기화
            taewoori.Initialize(this, assignedPosition);
            spawnedTaewoori = taewoori; // 생성된 태우리 참조 저장

            Debug.Log($"{gameObject.name}에서 태우리 즉시 생성 - 목표: {assignedPosition.name}");
        }
        else
        {
            Destroy(taewooliObj);
            Debug.LogError($"{gameObject.name}: ExitTaewoori 컴포넌트 없음");
        }
    }
    #endregion

    #region 태우리 관리 (ExitTaewoori에서 호출)
    /// <summary>
    /// 태우리가 제거될 때 호출
    /// </summary>
    public void OnTaewooliDestroyed(ExitTaewoori taewoori)
    {
        Debug.Log($"{gameObject.name}: 태우리 제거됨 - 파티클도 비활성화");

        // 생성된 태우리 참조 해제
        if (spawnedTaewoori == taewoori)
        {
            spawnedTaewoori = null;
        }

        // 태우리가 죽으면 파티클도 비활성화
        SetActive(false);
    }

    /// <summary>
    /// 플레이어 카메라 반환 (ExitTaewoori에서 사용)
    /// </summary>
    public Camera GetPlayerCamera()
    {
        return playerCamera;
    }
    #endregion

    #region 퍼블릭 메서드
    /// <summary>
    /// 즉시 태우리 생성 (테스트용)
    /// </summary>
    [ContextMenu("즉시 태우리 생성")]
    public void SpawnNow()
    {
        if (!hasSpawned)
        {
            if (autoAssignPosition)
            {
                AssignPosition();
            }
            SetActiveComplete(true); // 전체 활성화
            SpawnTaewoori();
            hasSpawned = true;
        }
    }

    /// <summary>
    /// 생성 상태 초기화
    /// </summary>
    public void ResetSpawnState()
    {
        hasSpawned = false;
        spawnedTaewoori = null;
        ReleaseAssignedPosition();
    }

    /// <summary>
    /// 강제로 전체 활성화 (테스트용)
    /// </summary>
    [ContextMenu("전체 활성화")]
    public void ForceActivateAll()
    {
        SetActiveComplete(true);
        isActive = true;
    }

    /// <summary>
    /// 강제로 전체 비활성화 (테스트용)
    /// </summary>
    [ContextMenu("전체 비활성화")]
    public void ForceDeactivateAll()
    {
        SetActiveComplete(false);
        isActive = false;
    }
    #endregion
}
