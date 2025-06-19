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

    [Header("생성 위치 설정")]
    [SerializeField] private bool useCustomPosition = false; // 커스텀 위치 사용 여부
    [SerializeField] private Vector3 spawnOffset = Vector3.zero; // 현재 오브젝트 기준 오프셋
    [SerializeField] private bool lookAtPlayer = true; // 플레이어를 바라보도록 설정

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

    private FloorManager floorManager;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 최종 생성 위치 계산
    /// </summary>
    private Vector3 FinalSpawnPosition
    {
        get
        {
            if (useCustomPosition)
            {
                return transform.position + spawnOffset;
            }
            else if (assignedPosition != null)
            {
                return assignedPosition.position;
            }
            else
            {
                return transform.position;
            }
        }
    }

    /// <summary>
    /// 최종 생성 회전값 계산
    /// </summary>
    private Quaternion FinalSpawnRotation
    {
        get
        {
            if (lookAtPlayer && playerCamera != null)
            {
                Vector3 spawnPos = FinalSpawnPosition;
                Vector3 directionToPlayer = (playerCamera.transform.position - spawnPos).normalized;
                directionToPlayer.y = 0; // Y축 회전만 적용 (위아래 회전 제거)

                if (directionToPlayer != Vector3.zero)
                {
                    return Quaternion.LookRotation(directionToPlayer);
                }
            }

            // 플레이어를 바라보지 않는 경우 기존 로직
            if (assignedPosition != null && !useCustomPosition)
            {
                return assignedPosition.rotation;
            }
            else
            {
                return transform.rotation;
            }
        }
    }
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
        }
    }
    #endregion

    #region 매니저 전용 활성화 메서드
    /// <summary>
    /// 매니저에서 호출 - 즉시 활성화 및 태우리 생성
    /// </summary>
    public void ActivateImmediately()
    {
        // 커스텀 위치를 사용하지 않을 때만 자동 배정
        if (!useCustomPosition && autoAssignPosition)
        {
            AssignPosition();
        }

        // 전체 오브젝트 활성화 (부모와 자식 모두)
        SetActiveComplete(true);

        // 즉시 태우리 생성
        if (!hasSpawned)
        {
            SpawnTaewoori();
            hasSpawned = true;
        }

        isActive = true;
    }
    #endregion

    #region 활성화/비활성화
    /// <summary>
    /// 파티클 및 태우리 생성 활성화/비활성화
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;

        // 전체 오브젝트 활성화/비활성화 (부모와 자식 모두)
        SetActiveComplete(active);

        if (!active)
        {
            // 비활성화 시 위치 해제 (커스텀 위치 사용 시에는 해제하지 않음)
            if (!useCustomPosition)
            {
                ReleaseAssignedPosition();
            }
        }
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
    }

    /// <summary>
    /// 자식 오브젝트들만 활성화/비활성화
    /// </summary>
    private void SetChildrenActive(bool active)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// 빈 위치 자동 배정
    /// </summary>
    private void AssignPosition()
    {
        if (allTaewooliPositions == null || allTaewooliPositions.Length == 0)
        {
            return;
        }

        // 빈 위치 찾기
        for (int i = 0; i < allTaewooliPositions.Length; i++)
        {
            if (!positionOccupied[i] && allTaewooliPositions[i] != null)
            {
                positionOccupied[i] = true;
                assignedPosition = allTaewooliPositions[i];
                return;
            }
        }
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
            Debug.LogWarning($"{gameObject.name}: ExitTaewoori 프리팹이 설정되지 않았습니다.");
            return;
        }

        // 최종 생성 위치와 회전값 계산
        Vector3 spawnPos = FinalSpawnPosition;
        Quaternion spawnRot = FinalSpawnRotation;

        // 태우리 생성
        GameObject taewooliObj = Instantiate(exitTaewooriPrefab, spawnPos, spawnRot);
        ExitTaewoori taewoori = taewooliObj.GetComponent<ExitTaewoori>();

        if (taewoori != null)
        {
            // 커스텀 위치 사용 시에는 이동하지 않도록 설정
            if (useCustomPosition)
            {
                // 커스텀 위치에서 생성하고 이동하지 않음
                taewoori.Initialize(this, null); // null로 전달하여 이동 방지
            }
            else
            {
                // 기존 방식: 배정된 위치로 이동
                taewoori.Initialize(this, assignedPosition);
            }

            spawnedTaewoori = taewoori; // 생성된 태우리 참조 저장

            Debug.Log($"{gameObject.name}: ExitTaewoori 생성 완료 - 위치: {spawnPos}");
        }
        else
        {
            Debug.LogError($"{gameObject.name}: ExitTaewoori 컴포넌트를 찾을 수 없습니다.");
            Destroy(taewooliObj);
        }
    }
    #endregion

    #region 런타임 위치 설정
    /// <summary>
    /// 런타임에서 생성 오프셋 변경
    /// </summary>
    public void SetSpawnOffset(Vector3 offset)
    {
        spawnOffset = offset;
    }

    /// <summary>
    /// 커스텀 위치 사용 여부 토글
    /// </summary>
    public void SetUseCustomPosition(bool useCustom)
    {
        useCustomPosition = useCustom;
    }

    /// <summary>
    /// 플레이어 바라보기 기능 토글
    /// </summary>
    public void SetLookAtPlayer(bool lookAt)
    {
        lookAtPlayer = lookAt;
    }

    /// <summary>
    /// 현재 설정된 생성 위치 반환
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        return FinalSpawnPosition;
    }

    /// <summary>
    /// 현재 설정된 생성 회전값 반환
    /// </summary>
    public Quaternion GetSpawnRotation()
    {
        return FinalSpawnRotation;
    }
    #endregion

    #region 태우리 관리 (ExitTaewoori에서 호출)
    /// <summary>
    /// 태우리가 제거될 때 호출
    /// </summary>
    public void OnTaewooliDestroyed(ExitTaewoori taewoori)
    {
        // FloorManager에 처치 알림
        if (floorManager != null)
        {
            floorManager.OnTaewooliKilled();
        }

        // 기존 코드...
        if (spawnedTaewoori == taewoori)
        {
            spawnedTaewoori = null;
        }
        SetActive(false);
    }

    public void SetFloorManager(FloorManager manager)
    {
        floorManager = manager;
    }

    /// <summary>
    /// 플레이어 카메라 반환 (ExitTaewoori에서 사용)
    /// </summary>
    public Camera GetPlayerCamera()
    {
        return playerCamera;
    }
    #endregion
}
