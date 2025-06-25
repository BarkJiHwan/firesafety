using System.Collections;
using UnityEngine;

/// <summary>
/// 화재/예방 씬 전용 소백이 - GameManager 연동 및 상호작용 시스템
/// </summary>
public class Sobaek : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("기본 위치 설정")]
    [SerializeField] private float offsetX = 1.5f;
    [SerializeField] private float offsetY = 0.5f;
    [SerializeField] private float offsetZ = 0.5f;
    [SerializeField] private bool stayOnRightSide = true;

    [Header("둥둥 떠다니기 효과")]
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float lookAtSpeed = 2f;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float arrivalDistance = 0.5f;

    [Header("생성 설정")]
    [SerializeField] private bool startInactive = true; // 비활성화 상태로 시작
    #endregion

    #region 프로퍼티
    public static Sobaek Instance { get; private set; }
    public Transform Player { get => playerTransform; set => playerTransform = value; }
    public bool IsMoving => isMovingToTarget || isMovingToHome;
    public bool IsTalking => isTalking;
    public bool SobaekInteractionEnabled
    {
        get => sobaekInteractionEnabled;
        set
        {
            sobaekInteractionEnabled = value;
            if (!value)
            {
                StopTalkingAndReturnHome();
            }
        }
    }
    #endregion

    #region 변수 선언
    private Transform playerTransform;
    private Vector3 homePosition;
    private Vector3 basePosition;
    private Vector3 targetPosition;
    private Transform currentTarget;
    private Animator animator;

    private bool isMovingToTarget = false;
    private bool isMovingToHome = false;
    private bool isTalking = false;
    private bool sobaekInteractionEnabled = true;

    private float floatTimer = 0f;

    // 애니메이션 해시
    private readonly int hashIsFlying = Animator.StringToHash("isFlying");
    private readonly int hashIsTalking = Animator.StringToHash("isTalking");
    private readonly int hashBackJump = Animator.StringToHash("BackJump");
    #endregion

    #region 유니티 라이프사이클
    /// <summary>
    /// 싱글톤 초기화
    /// </summary>
    void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// 컴포넌트 초기화 및 이벤트 등록
    /// </summary>
    void Start()
    {
        InitializeComponents();
        SetupInitialPosition();
        SubscribeToGameManagerEvents();
    }

    /// <summary>
    /// 매 프레임 이동 및 효과 업데이트
    /// </summary>
    void LateUpdate()
    {
        UpdateMovementAndEffects();
    }

    /// <summary>
    /// 이벤트 해제 및 싱글톤 정리
    /// </summary>
    void OnDestroy()
    {
        UnsubscribeFromGameManagerEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 싱글톤 패턴 초기화
    /// </summary>
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 컴포넌트 참조 초기화
    /// </summary>
    private void InitializeComponents()
    {
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// 초기 위치 설정
    /// </summary>
    private void SetupInitialPosition()
    {
        if (playerTransform != null)
        {
            SetHomePosition();
            basePosition = homePosition;
            transform.position = homePosition;
        }
    }
    #endregion

    #region 게임 페이즈 관리
    /// <summary>
    /// GameManager 이벤트 구독
    /// </summary>
    private void SubscribeToGameManagerEvents()
    {
        // GameManager가 있으면 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            // 현재 페이즈도 확인 (NowPhase 사용)
            OnPhaseChanged(GameManager.Instance.NowPhase);
        }
    }

    /// <summary>
    /// GameManager 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromGameManagerEvents()
    {
        // GameManager가 있으면 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    /// <summary>
    /// 게임 페이즈 변경 시 상호작용 활성화/비활성화
    /// </summary>
    private void OnPhaseChanged(GamePhase newPhase)
    {
        switch (newPhase)
        {
            case GamePhase.Prevention:
                sobaekInteractionEnabled = true;
                break;
            default:
                sobaekInteractionEnabled = false;
                StopTalkingAndReturnHome();
                break;
        }
    }
    #endregion

    #region 위치 및 이동
    /// <summary>
    /// 플레이어 기준 홈 포지션 계산
    /// </summary>
    private void SetHomePosition()
    {
        if (playerTransform == null)
            return;

        Vector3 rightDirection = playerTransform.right * (stayOnRightSide ? offsetX : -offsetX);
        Vector3 forwardDirection = playerTransform.forward * offsetZ;
        homePosition = playerTransform.position + rightDirection + forwardDirection + Vector3.up * offsetY;
    }

    /// <summary>
    /// 이동 및 효과 업데이트
    /// </summary>
    private void UpdateMovementAndEffects()
    {
        UpdatePosition();
        UpdateFloatingEffect();
        UpdateAnimations();
    }

    /// <summary>
    /// 위치 업데이트 (상태에 따른 이동)
    /// </summary>
    private void UpdatePosition()
    {
        if (playerTransform == null)
            return;

        if (isMovingToTarget)
        {
            MoveToTarget();
        }
        else if (isMovingToHome)
        {
            MoveToHome();
        }
        else if (!isTalking)
        {
            FollowPlayer();
        }
    }

    /// <summary>
    /// 타겟 위치로 이동
    /// </summary>
    private void MoveToTarget()
    {
        basePosition = Vector3.Slerp(basePosition, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(basePosition, targetPosition) <= arrivalDistance)
        {
            basePosition = targetPosition;
            isMovingToTarget = false;
            isTalking = true;
        }
    }

    /// <summary>
    /// 홈 위치로 이동
    /// </summary>
    private void MoveToHome()
    {
        basePosition = Vector3.Slerp(basePosition, homePosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(basePosition, homePosition) <= arrivalDistance)
        {
            basePosition = homePosition;
            isMovingToHome = false;
        }
    }

    /// <summary>
    /// 플레이어 따라가기
    /// </summary>
    private void FollowPlayer()
    {
        SetHomePosition();
        basePosition = Vector3.Slerp(basePosition, homePosition, followSpeed * Time.deltaTime);
    }
    #endregion

    #region 떠다니기 효과 및 애니메이션
    /// <summary>
    /// 둥둥 떠다니기 효과 및 회전 업데이트
    /// </summary>
    private void UpdateFloatingEffect()
    {
        floatTimer += Time.deltaTime * floatSpeed;
        float floatY = Mathf.Sin(floatTimer) * floatAmplitude;
        transform.position = basePosition + Vector3.up * floatY;

        UpdateLookDirection();
    }

    /// <summary>
    /// 바라볼 방향 업데이트
    /// </summary>
    private void UpdateLookDirection()
    {
        Vector3 targetDirection = GetLookDirection();

        if (targetDirection != Vector3.zero)
        {
            targetDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 상황에 따른 바라볼 방향 계산
    /// </summary>
    private Vector3 GetLookDirection()
    {
        if (isMovingToTarget && currentTarget != null)
        {
            return (currentTarget.position - transform.position).normalized;
        }
        else if (playerTransform != null)
        {
            return (playerTransform.position - transform.position).normalized;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 애니메이션 상태 업데이트
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null)
            return;

        bool isFlying = isMovingToTarget || isMovingToHome;
        animator.SetBool(hashIsFlying, isFlying);
        animator.SetBool(hashIsTalking, isTalking);
    }
    #endregion

    #region 상호작용
    /// <summary>
    /// 타겟으로 이동하기
    /// </summary>
    public void MoveToTarget(Transform target)
    {
        if (!sobaekInteractionEnabled || target == null)
            return;

        SetupTargetMovement(target);
    }

    /// <summary>
    /// 타겟 이동 설정
    /// </summary>
    private void SetupTargetMovement(Transform target)
    {
        currentTarget = target;
        Vector3 directionFromTarget = (transform.position - target.position).normalized;
        if (directionFromTarget == Vector3.zero)
            directionFromTarget = Vector3.up;

        targetPosition = target.position + directionFromTarget * 0.5f;
        basePosition = transform.position;

        isMovingToTarget = true;
        isMovingToHome = false;
        isTalking = false;
    }

    /// <summary>
    /// 대화 중단하고 홈으로 돌아가기
    /// </summary>
    public void StopTalkingAndReturnHome()
    {
        isTalking = false;
        isMovingToTarget = false;
        isMovingToHome = true;
        currentTarget = null;

        SetHomePosition();
        basePosition = transform.position;
    }

    /// <summary>
    /// 대화 시작
    /// </summary>
    public void StartTalking()
    {
        isTalking = true;
    }

    /// <summary>
    /// 대화 중단
    /// </summary>
    public void StopTalking()
    {
        isTalking = false;
    }

    /// <summary>
    /// 홈으로 돌아가기
    /// </summary>
    public void ReturnHome()
    {
        isMovingToTarget = false;
        isMovingToHome = true;
        isTalking = false;
        currentTarget = null;
    }
    #endregion

    #region 소백이 관리
    /// <summary>
    /// 소백이 활성화/비활성화 설정
    /// </summary>
    public void SetSobaekActive(bool active)
    {
        if (active)
        {
            ActivateSobaek();
        }
        else
        {
            DeactivateSobaek();
        }
    }

    /// <summary>
    /// 소백이 활성화
    /// </summary>
    private void ActivateSobaek()
    {
        gameObject.SetActive(true);

        if (playerTransform != null)
        {
            SetHomePosition();
            basePosition = homePosition;
            transform.position = homePosition;
        }
    }

    /// <summary>
    /// 소백이 비활성화 (애니메이션 후)
    /// </summary>
    private void DeactivateSobaek()
    {
        if (animator != null)
        {
            animator.SetTrigger(hashBackJump);
        }
        StartCoroutine(DeactivateAfterAnimation());
    }

    /// <summary>
    /// 애니메이션 재생 후 비활성화
    /// </summary>
    private IEnumerator DeactivateAfterAnimation()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
    #endregion
}
