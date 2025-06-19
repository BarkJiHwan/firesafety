using System.Collections;
using UnityEngine;


public class Sobaek : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("기본 위치 설정")]
    [SerializeField] private Transform player; // 플레이어 프리팹 직접 할당
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

    [Header("VR 최적화 설정")]
    [SerializeField] private bool usePhysicsMovement = false;
    [SerializeField] private float followCameraSpeed = 10f;

    [Header("애니메이션 설정")]
    [SerializeField] private GameObject sobaekCar;

    [Header("테스트용 설정")]
    [SerializeField] private bool testActivateCar = false;
    #endregion

    #region 프로퍼티
    public static Sobaek Instance { get; private set; }
    public Transform Player { get => player; set => player = value; }
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
    public bool UseGameManager { get; private set; }
    #endregion

    #region 변수 선언
    private Vector3 homePosition;
    private Vector3 basePosition;
    private Vector3 targetPosition;
    private Transform currentTarget;
    private Rigidbody rb;
    private Animator animator;

    private bool isMovingToTarget = false;
    private bool isMovingToHome = false;
    private bool isTalking = false;
    private bool sobaekInteractionEnabled = true;

    private float floatTimer = 0f;
    private GamePhase lastPhase;

    // 애니메이션 해시
    private readonly int hashIsFlying = Animator.StringToHash("isFlying");
    private readonly int hashIsTalking = Animator.StringToHash("isTalking");
    private readonly int hashBackJump = Animator.StringToHash("BackJump");
    #endregion

    #region 유니티 라이프사이클
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeComponents();

        // 플레이어가 설정되어 있는지 확인 후 홈 포지션 설정
        if (player != null)
        {
            SetHomePosition();
            basePosition = homePosition;
            transform.position = homePosition;
        }
        else
        {
            Debug.LogWarning("Sobaek: Player가 설정되지 않았습니다. 인스펙터에서 Player 필드를 설정해주세요.");
            // 플레이어가 없으면 현재 위치를 홈으로 설정
            basePosition = transform.position;
        }

        // 소백카 초기 비활성화
        if (sobaekCar != null)
        {
            sobaekCar.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (UseGameManager)
        {
            CheckGamePhase();
        }

        if (testActivateCar)
        {
            testActivateCar = false;
            ActivateSobaekCar();
        }

        if (!usePhysicsMovement)
        {
            UpdatePosition();
        }

        UpdateFloatingEffect();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (usePhysicsMovement)
        {
            UpdatePhysicsPosition();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 모든 컴포넌트 자동 초기화
    /// </summary>
    void InitializeComponents()
    {
        // 애니메이터 자동 찾기
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // 게임매니저 사용 여부 자동 감지
        DetectGameManagerUsage();

        // 물리 설정 초기화
        InitializePhysics();
    }

    /// <summary>
    /// 게임매니저 사용 여부 자동 감지
    /// </summary>
    void DetectGameManagerUsage()
    {
        // GameManager가 씬에 있는지 확인
        UseGameManager = GameManager.Instance != null;

        if (!UseGameManager)
        {
            // 게임매니저 없는 씬에서는 상호작용 비활성화
            sobaekInteractionEnabled = false;
            isMovingToTarget = false;
            isMovingToHome = false;
            currentTarget = null;

            Debug.Log("Sobaek: 게임매니저가 없는 씬으로 감지되었습니다. 상호작용이 비활성화됩니다.");
        }
        else
        {
            Debug.Log("Sobaek: 게임매니저가 있는 씬으로 감지되었습니다.");
        }
    }

    /// <summary>
    /// 물리 설정 초기화
    /// </summary>
    void InitializePhysics()
    {
        if (usePhysicsMovement)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.useGravity = false;
            rb.drag = 5f;
            rb.angularDrag = 5f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
    #endregion

    #region 게임 페이즈 감지
    /// <summary>
    /// 게임 페이즈 변경 감지 및 처리
    /// </summary>
    void CheckGamePhase()
    {
        // GameManager 재확인 (씬 전환 등으로 사라질 수 있음)
        if (GameManager.Instance == null)
        {
            UseGameManager = false;
            return;
        }

        GamePhase currentPhase = GameManager.Instance.CurrentPhase;

        // 페이즈가 변경되었을 때만 처리
        if (currentPhase != lastPhase)
        {
            if (currentPhase == GamePhase.Fire)
            {
                // 화재 페이즈: 상호작용 비활성화, 홈으로 복귀
                sobaekInteractionEnabled = false;
                StopTalkingAndReturnHome();
            }
            else if (currentPhase == GamePhase.Prevention)
            {
                // 예방 페이즈: 상호작용 활성화
                sobaekInteractionEnabled = true;
            }

            lastPhase = currentPhase;
        }
    }
    #endregion

    #region 위치 및 이동 (Transform 기반)
    void SetHomePosition()
    {
        if (player == null)
        {
            Debug.LogWarning("Sobaek: Player가 null입니다. 홈 포지션을 설정할 수 없습니다.");
            return;
        }

        Vector3 rightDirection = player.right * (stayOnRightSide ? offsetX : -offsetX);
        Vector3 forwardDirection = player.forward * offsetZ;
        homePosition = player.position + rightDirection + forwardDirection + Vector3.up * offsetY;
    }

    void UpdatePosition()
    {
        if (player == null)
            return;

        if (isMovingToTarget)
        {
            // 타겟으로 이동 - 부드러운 보간
            basePosition = Vector3.Slerp(basePosition, targetPosition, moveSpeed * Time.deltaTime);

            // 도착 체크
            if (Vector3.Distance(basePosition, targetPosition) <= arrivalDistance)
            {
                basePosition = targetPosition;
                isMovingToTarget = false;
                isTalking = true;
            }
        }
        else if (isMovingToHome)
        {
            // 홈으로 이동 - 부드러운 보간
            basePosition = Vector3.Slerp(basePosition, homePosition, moveSpeed * Time.deltaTime);

            // 도착 체크
            if (Vector3.Distance(basePosition, homePosition) <= arrivalDistance)
            {
                basePosition = homePosition;
                isMovingToHome = false;
            }
        }
        else if (!isMovingToTarget && !isMovingToHome && !isTalking)
        {
            // 평상시 플레이어 따라다니기
            SetHomePosition();
            basePosition = Vector3.Slerp(basePosition, homePosition, followSpeed * Time.deltaTime);
        }
    }
    #endregion

    #region 물리 기반 이동 (Rigidbody 사용)
    void UpdatePhysicsPosition()
    {
        if (player == null || rb == null)
            return;

        Vector3 targetPos = homePosition;

        if (isMovingToTarget)
        {
            targetPos = targetPosition;

            if (Vector3.Distance(transform.position, targetPosition) <= arrivalDistance)
            {
                rb.velocity = Vector3.zero;
                isMovingToTarget = false;
                isTalking = true;
                return;
            }
        }
        else if (isMovingToHome)
        {
            targetPos = homePosition;

            if (Vector3.Distance(transform.position, homePosition) <= arrivalDistance)
            {
                rb.velocity = Vector3.zero;
                isMovingToHome = false;
                return;
            }
        }
        else if (!isTalking)
        {
            // 평상시 플레이어 따라다니기
            SetHomePosition();
            targetPos = homePosition;
        }

        // 물리 기반 이동
        Vector3 direction = (targetPos - transform.position).normalized;
        float currentSpeed = isMovingToTarget || isMovingToHome ? moveSpeed : followSpeed;

        rb.velocity = Vector3.Lerp(rb.velocity, direction * currentSpeed, followCameraSpeed * Time.fixedDeltaTime);
        basePosition = transform.position;
    }
    #endregion

    #region 떠다니기 효과 및 회전
    void UpdateFloatingEffect()
    {
        floatTimer += Time.deltaTime * floatSpeed;
        float floatY = Mathf.Sin(floatTimer) * floatAmplitude;

        if (usePhysicsMovement)
        {
            Vector3 currentPos = transform.position;
            currentPos.y = basePosition.y + floatY;
            transform.position = currentPos;
        }
        else
        {
            transform.position = basePosition + Vector3.up * floatY;
        }

        UpdateLookDirection();
    }

    void UpdateLookDirection()
    {
        Vector3 targetDirection = Vector3.zero;

        if (isMovingToTarget && currentTarget != null)
        {
            targetDirection = (currentTarget.position - transform.position).normalized;
        }
        else if (player != null)
        {
            targetDirection = (player.position - transform.position).normalized;
        }

        if (targetDirection != Vector3.zero)
        {
            targetDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null)
            return;

        bool isFlying = isMovingToTarget || isMovingToHome;
        animator.SetBool(hashIsFlying, isFlying);
        animator.SetBool(hashIsTalking, isTalking);
    }
    #endregion

    #region 플레이어가 호출할 간단한 함수들
    /// <summary>
    /// 타겟으로 이동 (플레이어 호출용)
    /// </summary>
    public void MoveToTarget(Transform target)
    {
        if (!sobaekInteractionEnabled || target == null)
            return;

        currentTarget = target;
        Vector3 directionFromTarget = (transform.position - target.position).normalized;
        if (directionFromTarget == Vector3.zero)
            directionFromTarget = Vector3.up;

        targetPosition = target.position + directionFromTarget * 0.5f;

        isMovingToTarget = true;
        isMovingToHome = false;
        isTalking = false;
    }

    /// <summary>
    /// 토킹 중단하고 홈으로 복귀
    /// </summary>
    public void StopTalkingAndReturnHome()
    {
        isTalking = false;
        isMovingToTarget = false;
        isMovingToHome = true;
        currentTarget = null;
        SetHomePosition();
    }

    /// <summary>
    /// 토킹 애니매이션 실행
    /// </summary>
    public void StartTalking()
    {
        if (!UseGameManager || sobaekInteractionEnabled)
        {
            isTalking = true;
        }
    }

    /// <summary>
    /// 토킹애니매이션만 중단 (위치는 유지)
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

    #region 게임매니저 없는 씬 전용 메서드
    /// <summary>
    /// 토킹 애니메이션만 실행 (게임매니저 없는 씬용)
    /// </summary>
    public void PlayTalkingAnimation()
    {
        if (!UseGameManager)
        {
            isTalking = true;
        }
    }

    /// <summary>
    /// 토킹 애니메이션 중단 (게임매니저 없는 씬용)
    /// </summary>
    public void StopTalkingAnimation()
    {
        if (!UseGameManager)
        {
            isTalking = false;
        }
    }
    #endregion

    #region 런타임 설정 변경
    public void SetFollowSpeed(float speed)
    {
        followSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetPhysicsMovement(bool enabled)
    {
        usePhysicsMovement = enabled;

        if (enabled && rb == null)
        {
            InitializePhysics();
        }
        else if (!enabled && rb != null)
        {
            rb.velocity = Vector3.zero;
        }
    }
    #endregion

    #region 소백이 활성화/비활성화
    public void SetSobaekActive(bool active)
    {
        gameObject.SetActive(active);

        if (active)
        {
            if (player != null)
            {
                SetHomePosition();
                basePosition = homePosition;
                transform.position = homePosition;
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetTrigger(hashBackJump);
            }
            StartCoroutine(DeactivateAfterAnimation());
        }
    }

    private IEnumerator DeactivateAfterAnimation()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
    #endregion

    #region 소백카 관련 메서드
    /// <summary>
    /// 수건 입에 닿았을때 호출하면된다 한얼아
    /// </summary>
    public void ActivateSobaekCar()
    {
        if (sobaekCar != null)
        {
            sobaekCar.SetActive(true);
            gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// 플레이어 설정 후 홈 포지션 재설정
    /// </summary>
    public void RefreshHomePosition()
    {
        if (player != null)
        {
            SetHomePosition();
            basePosition = homePosition;
            transform.position = homePosition;
        }
    }
    #endregion
}
