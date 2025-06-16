using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Sobaek 방식으로 관리하는 ExitTaewoori - basePosition + 둥둥효과 분리
/// </summary>
public class ExitTaewoori : MonoBehaviour, IDamageable
{
    #region 인스펙터 설정
    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("이동 설정")]
    [SerializeField] private float floatingSpeed = 1f; // 둥둥 효과 속도
    [SerializeField] private float floatingHeight = 0.2f; // 둥둥 효과 높이
    [SerializeField] private float moveSpeed = 1f; // 플레이어 향해 이동 속도
    [SerializeField] private float rotationSpeed = 2f; // 회전 속도
    [SerializeField] private float stopDistance = 2f; // 플레이어에게서 멈출 거리
    #endregion

    #region 변수 선언
    private Vector3 basePosition; // 기준 위치 (이동만 담당)
    private Transform targetTransform; // 플레이어 카메라
    private FireThreatManager threatManager;
    private bool isDead = false;
    private float floatTimer = 0f; // 둥둥 효과용 타이머
    private int threatIndex = 0; // 부채꼴 배치용 인덱스
    #endregion

    #region 프로퍼티
    public FireThreatManager ThreatManager => threatManager;
    public bool IsDead => isDead;
    #endregion

    #region 유니티 라이프사이클
    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // 시작 위치를 기준 위치로 설정
        basePosition = transform.position;
        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.activated.AddListener(OnClicked);
        }
    }

    private void Update()
    {
        if (!isDead)
        {
            UpdateMovement(); // basePosition만 업데이트
            UpdateFloatingEffect(); // basePosition + 둥둥효과로 최종 위치 설정
            UpdateRotation(); // 회전 처리
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 초기화 - 매니저, 고정 위치, 이동 설정
    /// </summary>
    public void Initialize(FireThreatManager manager, Transform fixedPosition, float moveSpd, float rotSpd)
    {
        threatManager = manager;
        targetTransform = fixedPosition; // 고정 위치를 타겟으로 설정
        moveSpeed = moveSpd;
        rotationSpeed = rotSpd;

        currentHealth = maxHealth;
        isDead = false;

        // 현재 위치를 기준 위치로 설정
        basePosition = transform.position;
    }

    /// <summary>
    /// 초기화 - 매니저, 고정 위치 설정 (속도는 프리팹 값 사용)
    /// </summary>
    public void Initialize(FireThreatManager manager, Transform fixedPosition)
    {
        threatManager = manager;
        targetTransform = fixedPosition; // 고정 위치를 타겟으로 설정

        currentHealth = maxHealth;
        isDead = false;

        // 현재 위치를 기준 위치로 설정
        basePosition = transform.position;

        Debug.Log($"태우리 초기화 - 프리팹 설정값 사용 (이동: {moveSpeed}, 회전: {rotationSpeed})");
    }
    #endregion


    #region 이동 시스템 (Sobaek 방식)
    /// <summary>
    /// 기준 위치 업데이트 - 고정 위치로 이동
    /// </summary>
    private void UpdateMovement()
    {
        if (targetTransform == null)
            return;

        Vector3 currentPos = basePosition;
        Vector3 targetPos = targetTransform.position; // 고정 위치

        // 목표 위치까지의 거리 계산
        float distanceToTarget = Vector3.Distance(currentPos, targetPos);

        // 일정 거리(0.3m) 이상 떨어져 있으면 목표 위치로 이동
        if (distanceToTarget > 0.3f)
        {
            Vector3 moveDirection = (targetPos - currentPos).normalized;

            // basePosition 업데이트 - 고정 위치로 부드럽게 이동
            basePosition += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// 둥둥 떠다니는 효과 적용 (Sobaek 방식)
    /// </summary>
    private void UpdateFloatingEffect()
    {
        floatTimer += Time.deltaTime * floatingSpeed;
        float floatY = Mathf.Sin(floatTimer) * floatingHeight;

        // 최종 위치 = 기준 위치 + 둥둥 효과
        transform.position = basePosition + Vector3.up * floatY;
    }

    /// <summary>
    /// 회전 처리 (플레이어 바라보기)
    /// </summary>
    private void UpdateRotation()
    {
        if (threatManager == null || threatManager.PlayerCamera == null)
            return;

        // 플레이어 카메라 위치를 바라보기
        Vector3 playerPos = threatManager.PlayerCamera.transform.position;
        Vector3 lookDirection = (playerPos - transform.position);
        lookDirection.y = 0; // Y축 차이 무시 (수평으로만 회전)

        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }
    #endregion

    #region 데미지 시스템 (IDamageable 구현)

    void OnClicked(ActivateEventArgs args)
    {
        TakeDamage(25f); // 클릭하면 25 데미지
    }
    /// <summary>
    /// 데미지 처리 - IDamageable 인터페이스 구현
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    #endregion

    #region 사망 처리
    /// <summary>
    /// 사망 처리
    /// </summary>
    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 매니저에 사망 알림
        if (threatManager != null)
        {
            threatManager.OnThreatDestroyed(this);
        }

        Debug.Log($"{gameObject.name} 사망!");

        // 즉시 제거
        Destroy(gameObject);
    }
    #endregion

    #region 퍼블릭 메서드
    /// <summary>
    /// 타겟 설정
    /// </summary>
    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }

    /// <summary>
    /// 이동 속도 설정
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// 회전 속도 설정
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// 정지 거리 설정
    /// </summary>
    public void SetStopDistance(float distance)
    {
        stopDistance = Mathf.Max(0.5f, distance);
    }

    /// <summary>
    /// 즉시 제거
    /// </summary>
    public void ForceDestroy()
    {
        isDead = true;
        Destroy(gameObject);
    }

    /// <summary>
    /// 현재 체력 비율 반환
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }

    /// <summary>
    /// 플레이어와의 거리 반환
    /// </summary>
    public float GetDistanceToTarget()
    {
        if (targetTransform == null)
            return float.MaxValue;

        return Vector3.Distance(transform.position, targetTransform.position);
    }

    /// <summary>
    /// 위협 인덱스 설정 (부채꼴 배치용)
    /// </summary>
    public void SetThreatIndex(int index)
    {
        threatIndex = index;
    }
    #endregion
   
}
