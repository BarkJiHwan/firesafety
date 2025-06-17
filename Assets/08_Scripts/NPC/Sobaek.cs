using System.Collections;
using UnityEngine;

public class Sobaek : MonoBehaviour
{
   
    

    #region 인스펙터 설정
    [Header("기본 위치 설정")]
    [SerializeField] private Transform player;
    [SerializeField] private float offsetX = 1.5f;
    [SerializeField] private float offsetY = 0.5f;
    [SerializeField] private float offsetZ = 0.5f;
    [SerializeField] private bool stayOnRightSide = true;

    [Header("둥둥 떠다니기 효과")]
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float lookAtSpeed = 2f;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float arrivalDistance = 0.3f; // 도착 판정 거리

    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject sobaekCar;

    [Header("대피씬에서 체크 해제")]
    [SerializeField] private bool useGameManager = true; // 게임매니저 사용 여부

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
                // 비활성화 시 홈으로 복귀
                StopTalkingAndReturnHome();
            }
        }
    }
    public bool UseGameManager { get => useGameManager; set => useGameManager = value; }
    #endregion

    #region 변수 선언
    private Vector3 homePosition;
    private Vector3 basePosition;
    private Vector3 targetPosition;
    private Transform currentTarget;

    private bool isMovingToTarget = false;
    private bool isMovingToHome = false;
    private bool isTalking = false;
    private bool sobaekInteractionEnabled = true; // 소백이 상호작용 활성화 여부

    private float floatTimer = 0f;
    private GamePhase lastPhase; // 이전 페이즈 저장용

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

        InitializeReferences();
        SetHomePosition();
        basePosition = homePosition;

        // 소백카 초기 비활성화
        if (sobaekCar != null)
        {
            sobaekCar.SetActive(false);
        }
    }

    void Update()
    {
        if (useGameManager)
        {
            CheckGamePhase();
        }

        if (testActivateCar)
        {
            testActivateCar = false;
            ActivateSobaekCar();
        }

        UpdatePosition();
        UpdateFloatingEffect();
        UpdateAnimations();
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
    void InitializeReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (player == null)
        {
            // 1. MainCamera 태그로 찾기
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                player = mainCam.transform;
            }
            else
            {
                // 2. CenterEyeAnchor 이름으로 찾기 (VR)
                GameObject centerEye = GameObject.Find("CenterEyeAnchor");
                if (centerEye != null)
                {
                    player = centerEye.transform;
                }
                else
                {
                    // 3. Player 태그로 찾기
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null)
                    {
                        player = playerObj.transform;
                    }
                }
            }
        }
    }
    #endregion

    #region 게임 페이즈 감지
    /// <summary>
    /// 게임 페이즈 변경 감지 및 처리
    /// </summary>
    void CheckGamePhase()
    {
        // GameManager.Instance 접근 전에 null 체크
        if (GameManager.Instance == null)
        {
            // GameManager가 없으면 useGameManager를 false로 설정
            useGameManager = false;
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

    #region 위치 및 이동
    void SetHomePosition()
    {
        if (player == null)
            return;

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
            // 타겟으로 이동
            basePosition = Vector3.MoveTowards(basePosition, targetPosition, moveSpeed * Time.deltaTime);

            // 도착 체크
            if (Vector3.Distance(basePosition, targetPosition) <= arrivalDistance)
            {
                isMovingToTarget = false;
                isTalking = true; // 도착하면 자동으로 토킹 시작
            }
        }
        else if (isMovingToHome)
        {
            // 홈으로 이동 (홈 위치는 한 번만 계산)
            basePosition = Vector3.MoveTowards(basePosition, homePosition, moveSpeed * Time.deltaTime);

            // 도착 체크
            if (Vector3.Distance(basePosition, homePosition) <= arrivalDistance)
            {
                isMovingToHome = false;
            }
        }
        else if (!isMovingToTarget && !isMovingToHome && !isTalking)
        {
            // 평상시에만 홈 위치 추적 (이동 중도 토킹 중도 아닐 때만)
            SetHomePosition();
            basePosition = Vector3.Lerp(basePosition, homePosition, 3f * Time.deltaTime);
        }
    }

    void UpdateFloatingEffect()
    {
        floatTimer += Time.deltaTime * floatSpeed;
        float floatY = Mathf.Sin(floatTimer) * floatAmplitude;
        transform.position = basePosition + Vector3.up * floatY;

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

        // 이동 중이면 Flying
        bool isFlying = isMovingToTarget || isMovingToHome;
        animator.SetBool(hashIsFlying, isFlying);

        // 토킹 상태
        animator.SetBool(hashIsTalking, isTalking);
    }
    #endregion

    #region 플레이어가 호출할 간단한 함수들
    /// <summary>
    /// 타겟으로 이동 (플레이어 호출용) - 상호작용 활성화 상태에서만 작동
    /// </summary>
    public void MoveToTarget(Transform target)
    {
        // 상호작용이 비활성화되어 있으면 무시
        if (!sobaekInteractionEnabled)
        {
            return;
        }

        if (target == null)
            return;

        currentTarget = target;
        Vector3 directionFromTarget = (transform.position - target.position).normalized;
        if (directionFromTarget == Vector3.zero)
            directionFromTarget = Vector3.up;

        targetPosition = target.position + directionFromTarget * 0.5f;

        isMovingToTarget = true;
        isMovingToHome = false;
        isTalking = false; // 이동 시작하면 토킹 중단

    }

    /// <summary>
    /// 토킹 중단하고 홈으로 복귀 (플레이어 호출용)
    /// </summary>
    public void StopTalkingAndReturnHome()
    {
        isTalking = false;
        isMovingToTarget = false;
        isMovingToHome = true;
        currentTarget = null;

        // 홈 위치 미리 계산
        SetHomePosition();
    }

    /// <summary>
    /// 토킹 애니매이션 실행 UI 관련할때 쓰면됨
    /// </summary>
    public void StartTalking()
    {
        if (!sobaekInteractionEnabled)
            return;

        isTalking = true;
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

    #region 게임매니저 없는 씬 전용 메서드
    /// <summary>
    /// 토킹 애니메이션만 실행 (게임매니저 없는 씬용 - UI 설명시 사용)
    /// </summary>
    public void PlayTalkingAnimation()
    {
        if (!useGameManager)
        {
            isTalking = true;
        }
    }

    /// <summary>
    /// 토킹 애니메이션 중단 (게임매니저 없는 씬용)
    /// </summary>
    public void StopTalkingAnimation()
    {
        if (!useGameManager)
        {
            isTalking = false;
        }
    }

    /// <summary>
    /// 게임매니저 사용 여부 설정
    /// </summary>
    public void SetUseGameManager(bool use)
    {
        useGameManager = use;

        if (!use)
        {
            // 게임매니저 없는 씬에서는 상호작용 비활성화
            sobaekInteractionEnabled = false;
            isMovingToTarget = false;
            isMovingToHome = false;
            currentTarget = null;
        }
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
            // 소백카 활성화
            sobaekCar.SetActive(true);

            // 소백이 비활성화
            gameObject.SetActive(false);
        }
    }

    #endregion
}
