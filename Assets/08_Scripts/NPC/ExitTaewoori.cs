using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 새 구조용 ExitTaewoori - BaseTaewoori를 상속받아 애니메이션 시스템 활용
/// </summary>
public class ExitTaewoori : BaseTaewoori
{
    #region 인스펙터 설정
    [Header("이동 설정")]
    [SerializeField] private float floatingSpeed = 1f; // 둥둥 효과 속도
    [SerializeField] private float floatingHeight = 0.2f; // 둥둥 효과 높이
    [SerializeField] private float moveSpeed = 1f; // 플레이어 향해 이동 속도
    [SerializeField] private float rotationSpeed = 2f; // 회전 속도
    #endregion

    #region 변수 선언
    private Vector3 basePosition; // 기준 위치 (이동만 담당)
    private ExitTaewooliSpawnParticle spawnParticle; // 생성한 파티클 스크립트
    private float floatTimer = 0f; // 둥둥 효과용 타이머
    #endregion

    #region 프로퍼티
    public ExitTaewooliSpawnParticle SpawnParticle => spawnParticle;
    #endregion

    #region 유니티 라이프사이클
    protected override void Awake()
    {
        base.Awake(); // BaseTaewoori의 초기화 호출
    }

    private void Start()
    {
        // XR 인터랙션 설정
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
            UpdateFloatingEffect(); // basePosition + 둥둥효과로 최종 위치 설정
            UpdateRotation(); // 플레이어 바라보기
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 초기화 - 생성 파티클 설정
    /// </summary>
    public void Initialize(ExitTaewooliSpawnParticle particle)
    {
        spawnParticle = particle;

        // 기본 상태 리셋
        ResetState();

        // 현재 위치를 기준 위치로 설정
        basePosition = transform.position;
    }
    #endregion

    #region 이동 시스템
    /// <summary>
    /// 둥둥 떠다니는 효과 적용
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
        // 플레이어 카메라 찾기 (한 번만 실행되도록 최적화 가능)
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObj != null)
                playerCamera = cameraObj.GetComponent<Camera>();
        }

        if (playerCamera == null)
            return;

        // 플레이어 카메라 위치를 바라보기
        Vector3 playerPos = playerCamera.transform.position;
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

    #region 클릭 이벤트
    /// <summary>
    /// 클릭 공격 
    /// </summary>
    void OnClicked(ActivateEventArgs args)
    {
        TakeDamage(50f); // 클릭하면 50 데미지 (BaseTaewoori의 메서드 호출)
    }
    #endregion

    #region 사망 처리 오버라이드
    /// <summary>
    /// 사망 처리 - BaseTaewoori의 애니메이션 시스템 활용 후 파티클 알림
    /// </summary>
    public override void Die()
    {
        if (isDead)
            return;

        // BaseTaewoori의 Die() 호출 (애니메이션 재생 포함)
        base.Die();

        // 생성 파티클에 사망 알림
        if (spawnParticle != null)
        {
            spawnParticle.OnTaewooliDestroyed(this);
        }
    }

    /// <summary>
    /// 최종 사망 처리 오버라이드 - 오브젝트 파괴
    /// </summary>
    protected override void PerformFinalDeath()
    {
        // ExitTaewoori는 파괴하는 방식 사용
        Destroy(gameObject);
    }
    #endregion
}
