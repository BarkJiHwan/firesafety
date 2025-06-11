using System.Collections;
using UnityEngine;

public class Sobaek : MonoBehaviour
{
    #region 싱글톤
    public static Sobaek Instance { get; private set; }
    #endregion

    #region 인스펙터 설정
    [Header("기본 위치 설정")]
    [SerializeField] private Transform player; // VR 카메라 또는 플레이어 Transform
    [SerializeField] private float offsetX = 1.5f; // 플레이어와의 좌우 거리 (X축)
    [SerializeField] private float offsetY = 0.5f; // 플레이어 어깨 높이 (Y축)
    [SerializeField] private float offsetZ = 0.5f; // 플레이어 앞뒤 거리 (Z축, + = 앞쪽, - = 뒤쪽)
    [SerializeField] private bool stayOnRightSide = true; // 오른쪽에 고정

    [Header("둥둥 떠다니기 효과")]
    [SerializeField] private float floatAmplitude = 0.3f; // 위아래 움직임 크기
    [SerializeField] private float floatSpeed = 1f; // 위아래 움직임 속도
    [SerializeField] private float lookAtSpeed = 2f; // 플레이어 바라보는 회전 속도

    [Header("이동 설정")]
    [SerializeField] private float interactionMoveSpeed = 4f; // 상호작용 이동 속도
    [SerializeField] private float returnSpeed = 3f; // 돌아오는 속도
    [SerializeField] private float interactionOffset = 0.5f; // 상호작용 오브젝트에서 떨어진 거리
    [SerializeField] private float arrivalThreshold = 0.95f; // 도착 판정 (95% 지점)

    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator; // 소백이 애니메이터
    #endregion

    #region 변수 선언
    private Vector3 homePosition; // 기본 대기 위치
    private Vector3 basePosition; // 둥둥 효과 기준 위치
    private Vector3 interactionTarget; // 상호작용 타겟 위치
    private Vector3 moveStartPosition; // 이동 시작 위치
    private Vector3 currentMoveTarget; // 현재 이동 목표
    private Transform currentInteractionObject; // 현재 상호작용 중인 오브젝트

    private bool isInteracting = false; // 상호작용 중인지
    private bool isMovingToTarget = false; // 타겟으로 이동 중인지
    private bool isMovingToHome = false; // 홈으로 돌아가는 중인지
    private bool isHovering = false; // 토킹 중인지
    private bool hasArrivedAtTarget = false; // 타겟에 도착했는지

    private float currentMoveSpeed; // 현재 이동 속도
    private float moveProgress = 0f; // 이동 진행도 (0~1)
    private float floatTimer = 0f; // 둥둥 효과용 타이머

    // 애니메이션 해시 (성능 최적화)
    private readonly int hashIsFlying = Animator.StringToHash("isFlying"); // Bool로 변경
    private readonly int hashIsTalking = Animator.StringToHash("isTalking");
    private readonly int hashStartJump = Animator.StringToHash("StartJump");
    private readonly int hashBackJump = Animator.StringToHash("BackJump");
    #endregion

    #region 프로퍼티
    public Transform Player { get => player; set => player = value; }
    public bool IsInteracting => isInteracting;
    public bool IsHovering => isHovering;
    public bool IsMoving => isMovingToTarget || isMovingToHome;
    #endregion

    #region 유니티 라이프사이클
    void Start()
    {
        // 싱글톤 설정
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

        Debug.Log("[소백이] 초기화 완료");
    }

    void Update()
    {
        UpdatePosition();
        UpdateFloatingEffect();
        UpdateMovementProgress();
        UpdateAnimations();
    }

    void OnDestroy()
    {
        // 싱글톤 해제
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// VR 레퍼런스들 초기화
    /// </summary>
    void InitializeReferences()
    {
        // 애니메이터 자동 찾기
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        // 플레이어가 설정되지 않았다면 VR 카메라를 찾기
        if (player == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                player = mainCam.transform;
            }
            else
            {
                // Oculus/Meta Quest의 경우
                GameObject centerEye = GameObject.Find("CenterEyeAnchor");
                if (centerEye != null)
                {
                    player = centerEye.transform;
                }
            }
        }
    }
    #endregion

    #region 소백이 활성화/비활성화 시스템
    /// <summary>
    /// 소백이 활성화/비활성화 제어
    /// </summary>
    public void SetSobaekActive(bool active)
    {
        gameObject.SetActive(active);

        if (active)
        {
            // 홈 포지션 즉시 설정
            if (player != null)
            {
                SetHomePosition();
                basePosition = homePosition;
                transform.position = homePosition;
            }

            if (animator != null)
            {
                animator.SetTrigger(hashStartJump);
            }
            Debug.Log("[소백이] 활성화 완료");
        }
        else
        {
            if (animator != null)
            {
                animator.SetTrigger(hashBackJump);
            }
            StartCoroutine(DeactivateAfterAnimation());
            Debug.Log("[소백이] 비활성화 시작");
        }
    }

    /// <summary>
    /// BackJump 애니메이션 후 오브젝트 비활성화
    /// </summary>
    private IEnumerator DeactivateAfterAnimation()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
    #endregion

    #region 위치 및 이동 시스템
    /// <summary>
    /// 홈 포지션 설정 (플레이어 옆 고정 위치)
    /// </summary>
    void SetHomePosition()
    {
        if (player == null)
            return;

        // 좌우 방향 (X축)
        Vector3 rightDirection = player.right * (stayOnRightSide ? offsetX : -offsetX);

        // 앞뒤 방향 (Z축)
        Vector3 forwardDirection = player.forward * offsetZ;

        // 최종 홈 포지션 = 플레이어 위치 + 좌우오프셋 + 앞뒤오프셋 + 높이오프셋
        homePosition = player.position + rightDirection + forwardDirection + Vector3.up * offsetY;
    }

    /// <summary>
    /// 위치 업데이트 (홈 포지션 추적 또는 상호작용 이동)
    /// </summary>
    void UpdatePosition()
    {
        if (player == null)
            return;

        if (isMovingToTarget || isMovingToHome)
        {
            // 이동 중일 때는 Lerp로 이동
            basePosition = Vector3.Lerp(moveStartPosition, currentMoveTarget, moveProgress);
        }
        else
        {
            // 평상시에는 홈 포지션 추적
            SetHomePosition();
            basePosition = Vector3.Lerp(basePosition, homePosition, returnSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 이동 진행도 업데이트 및 도착 판정
    /// </summary>
    void UpdateMovementProgress()
    {
        if (!isMovingToTarget && !isMovingToHome)
            return;

        // 이동 진행도 계산 (속도 기반)
        float moveDistance = Vector3.Distance(moveStartPosition, currentMoveTarget);
        if (moveDistance > 0.01f)
        {
            moveProgress += (currentMoveSpeed * Time.deltaTime) / moveDistance;
            moveProgress = Mathf.Clamp01(moveProgress);
        }

        // 도착 판정 (95% 지점)
        if (moveProgress >= arrivalThreshold)
        {
            if (isMovingToTarget)
            {
                // 상호작용 타겟 도착
                isMovingToTarget = false;
                hasArrivedAtTarget = true;
                isHovering = true; // 토킹 시작
                moveProgress = 0f;

                Debug.Log("[소백이] 상호작용 타겟 도착 - 토킹 시작");
            }
            else if (isMovingToHome)
            {
                // 홈 포지션 도착
                isMovingToHome = false;
                hasArrivedAtTarget = false;
                moveProgress = 0f;

                Debug.Log("[소백이] 홈 포지션 도착");
            }
        }
    }

    /// <summary>
    /// 애니메이션 상태 업데이트
    /// </summary>
    void UpdateAnimations()
    {
        if (animator == null)
            return;

        // isFlying: 이동 중일 때만 true
        bool shouldFly = isMovingToTarget || isMovingToHome;
        animator.SetBool(hashIsFlying, shouldFly);

        // isTalking: 도착 후 호버링 중일 때만 true
        animator.SetBool(hashIsTalking, hasArrivedAtTarget && isHovering);
    }
    #endregion

    #region 둥둥 효과 및 회전
    /// <summary>
    /// 둥둥 떠다니는 효과
    /// </summary>
    void UpdateFloatingEffect()
    {
        floatTimer += Time.deltaTime * floatSpeed;

        // 위아래 움직임
        float floatY = Mathf.Sin(floatTimer) * floatAmplitude;
        Vector3 floatingPosition = basePosition + Vector3.up * floatY;

        // 위치 적용
        transform.position = floatingPosition;

        // 바라보는 방향 결정
        UpdateLookDirection();
    }

    /// <summary>
    /// 상황에 따라 바라보는 방향 업데이트
    /// </summary>
    void UpdateLookDirection()
    {
        Vector3 targetDirection = Vector3.zero;

        if (isMovingToTarget && currentInteractionObject != null)
        {
            // 오브젝트로 날아가는 중일 때만 오브젝트를 바라보기
            targetDirection = (currentInteractionObject.position - transform.position).normalized;
        }
        else if (player != null)
        {
            // 평상시, 상호작용 중, 홈으로 돌아갈 때는 모두 플레이어를 바라보기
            targetDirection = (player.position - transform.position).normalized;
        }

        // 바라보기 실행
        LookAtDirection(targetDirection);
    }

    /// <summary>
    /// 지정된 방향을 부드럽게 바라보기
    /// </summary>
    void LookAtDirection(Vector3 direction)
    {
        if (direction == Vector3.zero)
            return;

        // Y축 회전만 적용 (상하로 고개 너무 많이 돌리지 않게)
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                lookAtSpeed * Time.deltaTime
            );
        }
    }
    #endregion

    #region 상호작용 이동
    /// <summary>
    /// 상호작용 오브젝트로 이동 시작
    /// </summary>
    public void MoveToInteractionTarget(Transform target)
    {
        if (target == null)
            return;

        // 타겟에서 약간 떨어진 위치 계산
        Vector3 directionFromTarget = (transform.position - target.position).normalized;
        if (directionFromTarget == Vector3.zero)
        {
            directionFromTarget = Vector3.up;
        }

        interactionTarget = target.position + directionFromTarget * interactionOffset;
        currentInteractionObject = target;

        // 이동 시작 설정
        moveStartPosition = basePosition;
        currentMoveTarget = interactionTarget;
        currentMoveSpeed = interactionMoveSpeed;
        moveProgress = 0f;

        isInteracting = true;
        isMovingToTarget = true;
        isMovingToHome = false;
        isHovering = false;
        hasArrivedAtTarget = false;

        Debug.Log($"[소백이] {target.name}으로 이동 시작 - 속도: {currentMoveSpeed}");
    }

    /// <summary>
    /// 홈 포지션으로 돌아가기 시작
    /// </summary>
    public void StopInteraction()
    {
        // 홈 위치 업데이트
        SetHomePosition();

        // 이동 시작 설정
        moveStartPosition = basePosition;
        currentMoveTarget = homePosition;
        currentMoveSpeed = returnSpeed;
        moveProgress = 0f;

        isInteracting = false;
        isMovingToTarget = false;
        isMovingToHome = true;
        isHovering = false;
        hasArrivedAtTarget = false;
        currentInteractionObject = null;

        Debug.Log($"[소백이] 홈으로 복귀 시작 - 속도: {currentMoveSpeed}");
    }

    /// <summary>
    /// 컨트롤러 호버 시작 (이동만 시작)
    /// </summary>
    public void StartHovering()
    {
        Debug.Log("[소백이] 호버 시작 - 이동 시작");
    }

    /// <summary>
    /// 컨트롤러 호버 종료 (토킹 중단 및 복귀)
    /// </summary>
    public void StopHovering()
    {
        isHovering = false;
        Debug.Log("[소백이] 호버 종료 - 토킹 중단");
    }
    #endregion

    #region 기존 말하기 시스템 (수동 호출용)
    /// <summary>
    /// UI 담당자가 사용할 말하기 함수 (수동 호출용)
    /// </summary>
    /// <param name="duration">말하는 시간 (초)</param>
    public void StartTalking(float duration = 3f)
    {
        if (animator == null)
            return;

        StopAllCoroutines();
        StartCoroutine(TalkingRoutine(duration));
    }

    /// <summary>
    /// 말하기 애니메이션 코루틴 (수동 호출용)
    /// </summary>
    private IEnumerator TalkingRoutine(float duration)
    {
        bool originalHoverState = isHovering;

        isHovering = true;
        Debug.Log($"[소백이] 수동 말하기 시작 - {duration}초 동안");

        yield return new WaitForSeconds(duration);

        isHovering = originalHoverState;
        Debug.Log("[소백이] 수동 말하기 종료");
    }

    /// <summary>
    /// 말하기 중단
    /// </summary>
    public void StopTalking()
    {
        StopAllCoroutines();
        isHovering = false;
        Debug.Log("[소백이] 말하기 강제 중단");
    }
    #endregion

    #region 유틸리티 메서드
    /// <summary>
    /// 소백이를 원래 위치로 즉시 이동
    /// </summary>
    public void TeleportToPlayer()
    {
        if (player != null)
        {
            ResetMovementState();
            SetHomePosition();
            basePosition = homePosition;
            Debug.Log("[소백이] 플레이어 위치로 즉시 이동");
        }
    }

    /// <summary>
    /// 모든 이동 상태 리셋
    /// </summary>
    private void ResetMovementState()
    {
        isInteracting = false;
        isMovingToTarget = false;
        isMovingToHome = false;
        isHovering = false;
        hasArrivedAtTarget = false;
        currentInteractionObject = null;
        moveProgress = 0f;
    }

    /// <summary>
    /// 소백이 위치를 왼쪽/오른쪽으로 전환
    /// </summary>
    public void ToggleSide()
    {
        stayOnRightSide = !stayOnRightSide;
        SetHomePosition();
        Debug.Log($"[소백이] 위치 전환 - 오른쪽: {stayOnRightSide}");
    }
    #endregion

    #region 설정 변경 메서드
    public void SetFloatingEffect(float amplitude, float speed)
    {
        floatAmplitude = amplitude;
        floatSpeed = speed;
    }

    public void SetLookAtSpeed(float speed)
    {
        lookAtSpeed = Mathf.Clamp(speed, 0.1f, 10f);
    }

    public void SetInteractionMoveSpeed(float speed)
    {
        interactionMoveSpeed = Mathf.Clamp(speed, 0.5f, 10f);
    }

    public void SetReturnSpeed(float speed)
    {
        returnSpeed = Mathf.Clamp(speed, 0.5f, 10f);
    }

    public void SetArrivalThreshold(float threshold)
    {
        arrivalThreshold = Mathf.Clamp(threshold, 0.8f, 1f);
    }

    public void SetAllOffsets(float distance, float forward, float height)
    {
        offsetX = Mathf.Clamp(distance, 0.5f, 5f);
        offsetZ = Mathf.Clamp(forward, -3f, 3f);
        offsetY = Mathf.Clamp(height, -1f, 3f);
    }
    #endregion
}
