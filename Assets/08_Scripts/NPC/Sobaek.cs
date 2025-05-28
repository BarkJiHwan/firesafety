using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// VR 도우미 NPC 소백이 - 각 플레이어의 개인 전용 가이드
/// 항상 시야에 보이며 상호작용 가능한 오브젝트 근처로 이동하여 도움을 제공
/// </summary>
public class Sobaek : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("===== 기본 위치 설정 =====")]
    [SerializeField] private Vector3 defaultOffsetFromCamera = new Vector3(0.7f, -0.3f, 1.2f); // 카메라 기준 기본 위치
    [SerializeField] private float followSmoothSpeed = 3f; // 따라다니는 부드러움
    [SerializeField] private bool alwaysFacePlayer = true; // 항상 플레이어 바라보기

    [Header("===== 둥둥 떠다니기 효과 =====")]
    [SerializeField] private float floatAmplitude = 0.1f; // 위아래 움직임 크기
    [SerializeField] private float floatSpeed = 2f; // 둥둥 속도
    [SerializeField] private float rotateSpeed = 30f; // 자체 회전 속도

    [Header("===== 상호작용 감지 =====")]
    [SerializeField] private float detectionRange = 1.5f; // 상호작용 오브젝트 감지 범위
    [SerializeField] private LayerMask interactableLayer = 1; // 상호작용 가능한 레이어
    [SerializeField] private string interactableTag = "Interactable"; // 상호작용 가능한 태그

    [Header("===== 이동 설정 =====")]
    [SerializeField] private float moveSpeed = 2f; // 상호작용 오브젝트로 이동 속도
    [SerializeField] private float arrivalDistance = 0.5f; // 도착 판정 거리
    [SerializeField] private float maxDistanceFromPlayer = 5f; // 플레이어로부터 최대 거리

    [Header("===== VR 컨트롤러 참조 =====")]
    [SerializeField] private Transform playerCamera; // VR 카메라
    [SerializeField] private Transform leftController; // 왼손 컨트롤러
    [SerializeField] private Transform rightController; // 오른손 컨트롤러
    [SerializeField] private Transform playerTransform; // 플레이어 Transform
    #endregion

    #region 변수 선언
    // 상태 관리
    public enum SobaekState
    {
        Following,      // 기본 따라다니기
        MovingToTarget, // 상호작용 오브젝트로 이동
        AtTarget,       // 상호작용 오브젝트 근처 대기
        Returning       // 원래 위치로 돌아가기
    }

    private SobaekState currentState = SobaekState.Following;
    private Transform currentTarget; // 현재 목표 오브젝트
    private Vector3 basePosition; // 기본 위치 (둥둥 효과 기준점)
    private Vector3 targetPosition; // 목표 위치
    private float floatTimer = 0f; // 둥둥 효과 타이머

    // 컴포넌트 참조
    private PhotonView ownerPhotonView;
    private Collider[] detectedColliders = new Collider[10]; // 감지된 콜라이더 배열

    // 시각적 효과용
    private Vector3 initialScale;
    private Material sobaekMaterial;

    // 컴포넌트 찾기 재시도용
    private float findComponentsTimer = 0f;
    private float findComponentsInterval = 1f; // 1초마다 재시도
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 소백이 상태
    /// </summary>
    public SobaekState CurrentState => currentState;

    /// <summary>
    /// 현재 목표 오브젝트
    /// </summary>
    public Transform CurrentTarget => currentTarget;
    #endregion

    #region 유니티 라이프사이클
    void Start()
    {
        InitializeSobaek();

        Debug.Log("[소백이] 초기화 시작");
        FindVRComponents();

        // 초기 위치 설정
        if (playerCamera != null)
        {
            SetToDefaultPosition();
            Debug.Log("[소백이] 기본 위치 설정 완료");
        }
        else
        {
            Debug.LogWarning("[소백이] 플레이어 카메라를 찾을 수 없습니다. 재시도 중...");
        }
    }

    void Update()
    {
        // 컴포넌트를 찾지 못했다면 주기적으로 재시도
        if (!ValidateComponents())
        {
            findComponentsTimer += Time.deltaTime;
            if (findComponentsTimer >= findComponentsInterval)
            {
                findComponentsTimer = 0f;
                FindVRComponents();
            }
            return;
        }

        // 상태별 업데이트
        switch (currentState)
        {
            case SobaekState.Following:
                UpdateFollowing();
                break;
            case SobaekState.MovingToTarget:
                UpdateMovingToTarget();
                break;
            case SobaekState.AtTarget:
                UpdateAtTarget();
                break;
            case SobaekState.Returning:
                UpdateReturning();
                break;
        }

        // 공통 효과
        ApplyFloatingEffect();
        ApplyRotationEffect();

        // 상호작용 오브젝트 감지 (Following 상태일 때만)
        if (currentState == SobaekState.Following)
        {
            CheckForInteractables();
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 소백이 초기 설정
    /// </summary>
    private void InitializeSobaek()
    {
        initialScale = transform.localScale;

        // 머티리얼 가져오기 (시각적 효과용)
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            sobaekMaterial = renderer.material;
        }

        floatTimer = Random.Range(0f, Mathf.PI * 2); // 랜덤 시작점
    }

    /// <summary>
    /// VR 컴포넌트 자동 찾기 (수정된 버전)
    /// </summary>
    private void FindVRComponents()
    {
        // PlayerBehavior를 찾기 (PhotonView가 있음)
        PlayerBehavior playerBehavior = FindObjectOfType<PlayerBehavior>();

        if (playerBehavior != null && playerBehavior.photonView.IsMine)
        {
            Debug.Log("[소백이] PlayerBehavior 찾음: " + playerBehavior.name);

            // PlayerComponents 찾기
            PlayerComponents playerComponents = playerBehavior.GetComponent<PlayerComponents>();
            if (playerComponents != null)
            {
                playerTransform = playerBehavior.transform;

                // PlayerBehavior에 이미 카메라가 있음
                if (playerBehavior.playerCam != null)
                {
                    playerCamera = playerBehavior.playerCam.transform;
                    Debug.Log("[소백이] 카메라 할당됨: " + playerCamera.name);
                }

                // XR 컴포넌트에서 컨트롤러 찾기
                if (playerComponents.xRComponents != null)
                {
                    FindControllersInXR(playerComponents.xRComponents);
                }
                else
                {
                    Debug.LogWarning("[소백이] xRComponents가 null입니다.");
                }
            }
            else
            {
                Debug.LogWarning("[소백이] PlayerComponents를 찾을 수 없습니다.");
            }
        }
        else
        {
            if (playerBehavior == null)
                Debug.LogWarning("[소백이] PlayerBehavior를 찾을 수 없습니다.");
            else
                Debug.LogWarning("[소백이] PlayerBehavior가 내 플레이어가 아닙니다.");
        }
    }

    /// <summary>
    /// XR 컴포넌트에서 VR 컨트롤러 찾기
    /// </summary>
    private void FindControllersInXR(GameObject xrComponents)
    {
        // XR 컴포넌트 하위에서 컨트롤러 찾기
        Transform[] allChildren = xrComponents.GetComponentsInChildren<Transform>();

        Debug.Log("[소백이] XR 컴포넌트에서 " + allChildren.Length + "개 Transform 발견");

        foreach (Transform child in allChildren)
        {
            string childName = child.name.ToLower();
            Debug.Log("[소백이] 검사 중: " + child.name);

            // 왼쪽 컨트롤러 찾기 (다양한 명명 규칙 지원)
            if ((childName.Contains("left") && (childName.Contains("controller") || childName.Contains("hand"))) ||
                childName.Contains("lefthand") ||
                childName.Contains("left_hand") ||
                childName.Contains("l_hand") ||
                childName.Contains("hand_left"))
            {
                leftController = child;
                Debug.Log("[소백이] 왼쪽 컨트롤러 할당됨: " + child.name);
            }

            // 오른쪽 컨트롤러 찾기 (다양한 명명 규칙 지원)
            if ((childName.Contains("right") && (childName.Contains("controller") || childName.Contains("hand"))) ||
                childName.Contains("righthand") ||
                childName.Contains("right_hand") ||
                childName.Contains("r_hand") ||
                childName.Contains("hand_right"))
            {
                rightController = child;
                Debug.Log("[소백이] 오른쪽 컨트롤러 할당됨: " + child.name);
            }
        }

        if (leftController == null)
            Debug.LogWarning("[소백이] 왼쪽 컨트롤러를 찾을 수 없습니다.");
        if (rightController == null)
            Debug.LogWarning("[소백이] 오른쪽 컨트롤러를 찾을 수 없습니다.");
    }

    /// <summary>
    /// VR 컨트롤러 찾기 (VR 시스템별로 수정 필요) - 더 이상 사용하지 않음
    /// </summary>
    private void FindControllers(PlayerComponents playerComponents)
    {
        // 이 메서드는 FindControllersInXR로 대체됨
        if (playerComponents.xRComponents != null)
        {
            FindControllersInXR(playerComponents.xRComponents);
        }
    }

    /// <summary>
    /// 소백이 소유자 설정 (PlayerComponents에서 호출) - 수정된 버전
    /// </summary>
    public void SetOwner(PlayerComponents owner)
    {
        // PlayerComponents와 같은 GameObject에서 PhotonView 찾기
        PhotonView photonView = owner.GetComponent<PhotonView>();

        if (photonView == null)
        {
            Debug.LogError("[소백이] PlayerComponents에 PhotonView가 없습니다!");
            return;
        }

        ownerPhotonView = photonView;

        // 내 플레이어가 아니면 비활성화
        if (!ownerPhotonView.IsMine)
        {
            gameObject.SetActive(false);
            return;
        }

        // 내 플레이어면 VR 컴포넌트 설정
        playerTransform = owner.transform;
        FindVRComponents();
    }
    #endregion

    #region 상태별 업데이트
    /// <summary>
    /// 기본 따라다니기 상태 업데이트
    /// </summary>
    private void UpdateFollowing()
    {
        SetToDefaultPosition();

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition,
                                        followSmoothSpeed * Time.deltaTime);

        // 플레이어 바라보기
        if (alwaysFacePlayer && playerCamera != null)
        {
            Vector3 lookDirection = playerCamera.position - transform.position;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }

    /// <summary>
    /// 상호작용 오브젝트로 이동 중 업데이트
    /// </summary>
    private void UpdateMovingToTarget()
    {
        if (currentTarget == null)
        {
            ChangeState(SobaekState.Returning);
            return;
        }

        // 목표 지점으로 이동
        Vector3 targetPos = currentTarget.position + Vector3.up * 0.5f; // 오브젝트 위쪽
        transform.position = Vector3.MoveTowards(transform.position, targetPos,
                                               moveSpeed * Time.deltaTime);

        // 목표 바라보기
        Vector3 lookDirection = currentTarget.position - transform.position;
        lookDirection.y = 0; // Y축 회전 제거
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // 도착 체크
        if (Vector3.Distance(transform.position, targetPos) < arrivalDistance)
        {
            ChangeState(SobaekState.AtTarget);
        }

        // 플레이어와 너무 멀어지면 돌아가기
        if (playerCamera != null && Vector3.Distance(transform.position, playerCamera.position) > maxDistanceFromPlayer)
        {
            ChangeState(SobaekState.Returning);
        }
    }

    /// <summary>
    /// 상호작용 오브젝트 근처 대기 업데이트
    /// </summary>
    private void UpdateAtTarget()
    {
        if (currentTarget == null)
        {
            ChangeState(SobaekState.Returning);
            return;
        }

        // 상호작용 오브젝트 주변에서 둥둥 떠다니기
        Vector3 targetPos = currentTarget.position + Vector3.up * 0.5f;
        basePosition = targetPos;

        // 컨트롤러가 멀어지면 돌아가기
        if (!IsControllerNearTarget())
        {
            StartCoroutine(DelayedReturn(2f)); // 2초 후 돌아가기
        }
    }

    /// <summary>
    /// 원래 위치로 돌아가기 업데이트
    /// </summary>
    private void UpdateReturning()
    {
        SetToDefaultPosition();

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition,
                                        followSmoothSpeed * 2f * Time.deltaTime);

        // 플레이어 바라보기
        if (alwaysFacePlayer && playerCamera != null)
        {
            Vector3 lookDirection = playerCamera.position - transform.position;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        // 도착하면 Following 상태로
        if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
        {
            ChangeState(SobaekState.Following);
        }
    }
    #endregion

    #region 상호작용 감지
    /// <summary>
    /// VR 컨트롤러 주변의 상호작용 가능한 오브젝트 감지
    /// </summary>
    private void CheckForInteractables()
    {
        Transform nearestInteractable = null;
        float nearestDistance = float.MaxValue;

        // 왼쪽 컨트롤러 주변 체크
        if (leftController != null)
        {
            Transform found = FindInteractableNear(leftController.position);
            if (found != null)
            {
                float distance = Vector3.Distance(leftController.position, found.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInteractable = found;
                }
            }
        }

        // 오른쪽 컨트롤러 주변 체크
        if (rightController != null)
        {
            Transform found = FindInteractableNear(rightController.position);
            if (found != null)
            {
                float distance = Vector3.Distance(rightController.position, found.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInteractable = found;
                }
            }
        }

        // 상호작용 오브젝트 발견 시 이동
        if (nearestInteractable != null && nearestInteractable != currentTarget)
        {
            MoveToInteractable(nearestInteractable);
        }
    }

    /// <summary>
    /// 지정된 위치 주변의 상호작용 가능한 오브젝트 찾기
    /// </summary>
    private Transform FindInteractableNear(Vector3 position)
    {
        int count = Physics.OverlapSphereNonAlloc(position, detectionRange, detectedColliders, interactableLayer);

        for (int i = 0; i < count; i++)
        {
            Collider col = detectedColliders[i];

            // 태그로 상호작용 가능 여부 확인
            if (col.CompareTag(interactableTag))
            {
                return col.transform;
            }

            // 또는 특정 컴포넌트로 확인 (예: IInteractable 인터페이스)
            if (col.GetComponent<IInteractable>() != null)
            {
                return col.transform;
            }

            // FireObjScript가 있는 오브젝트도 상호작용 가능
            if (col.GetComponent<FireObjScript>() != null)
            {
                return col.transform;
            }
        }

        return null;
    }

    /// <summary>
    /// 컨트롤러가 현재 타겟 근처에 있는지 확인
    /// </summary>
    private bool IsControllerNearTarget()
    {
        if (currentTarget == null)
            return false;

        bool leftNear = leftController != null &&
                       Vector3.Distance(leftController.position, currentTarget.position) < detectionRange * 1.5f;
        bool rightNear = rightController != null &&
                        Vector3.Distance(rightController.position, currentTarget.position) < detectionRange * 1.5f;

        return leftNear || rightNear;
    }
    #endregion

    #region 이동 제어
    /// <summary>
    /// 상호작용 오브젝트로 이동 시작
    /// </summary>
    public void MoveToInteractable(Transform target)
    {
        if (target == null)
            return;

        currentTarget = target;
        ChangeState(SobaekState.MovingToTarget);

        Debug.Log($"[소백이] {target.name}으로 이동 시작");
    }

    /// <summary>
    /// 기본 위치로 돌아가기
    /// </summary>
    public void ReturnToPlayer()
    {
        currentTarget = null;
        ChangeState(SobaekState.Returning);

        Debug.Log("[소백이] 플레이어에게 돌아가기");
    }

    /// <summary>
    /// 기본 위치 설정
    /// </summary>
    private void SetToDefaultPosition()
    {
        if (playerCamera != null)
        {
            targetPosition = playerCamera.position +
                           playerCamera.TransformDirection(defaultOffsetFromCamera);
            basePosition = targetPosition;
        }
    }

    /// <summary>
    /// 상태 변경
    /// </summary>
    private void ChangeState(SobaekState newState)
    {
        if (currentState != newState)
        {
            Debug.Log($"[소백이] 상태 변경: {currentState} -> {newState}");
            currentState = newState;
        }
    }
    #endregion

    #region 시각적 효과
    /// <summary>
    /// 둥둥 떠다니는 효과 적용
    /// </summary>
    private void ApplyFloatingEffect()
    {
        floatTimer += Time.deltaTime * floatSpeed;

        Vector3 floatOffset = Vector3.up * Mathf.Sin(floatTimer) * floatAmplitude;
        transform.position = basePosition + floatOffset;
    }

    /// <summary>
    /// 자체 회전 효과 적용
    /// </summary>
    private void ApplyRotationEffect()
    {
        if (currentState == SobaekState.Following || currentState == SobaekState.AtTarget)
        {
            // Y축으로 천천히 회전
            transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.Self);
        }
    }

    /// <summary>
    /// 크기 변화 효과 (상호작용 시)
    /// </summary>
    public void PlayInteractionEffect()
    {
        StartCoroutine(ScalePulse());
    }

    private IEnumerator ScalePulse()
    {
        Vector3 targetScale = initialScale * 1.2f;
        float duration = 0.3f;
        float elapsed = 0f;

        // 크기 증가
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        // 원래 크기로
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(targetScale, initialScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = initialScale;
    }
    #endregion

    #region 유틸리티
    /// <summary>
    /// 컴포넌트 유효성 검사 (수정된 버전)
    /// </summary>
    private bool ValidateComponents()
    {
        if (playerCamera == null || playerTransform == null)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 지연된 돌아가기
    /// </summary>
    private IEnumerator DelayedReturn(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentState == SobaekState.AtTarget && !IsControllerNearTarget())
        {
            ReturnToPlayer();
        }
    }
    #endregion

    #region 디버그
    /// <summary>
    /// 기즈모 그리기 (디버그용)
    /// </summary>
    private void OnDrawGizmos()
    {
        // 감지 범위 표시
        if (leftController != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(leftController.position, detectionRange);
        }

        if (rightController != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rightController.position, detectionRange);
        }

        // 현재 타겟으로의 라인
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }

        // 플레이어와의 거리 표시
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, playerCamera.position);
        }
    }

    /// <summary>
    /// 수동 테스트용 함수들
    /// </summary>
    [ContextMenu("플레이어에게 돌아가기")]
    public void TestReturnToPlayer()
    {
        ReturnToPlayer();
    }

    [ContextMenu("상호작용 효과 재생")]
    public void TestInteractionEffect()
    {
        PlayInteractionEffect();
    }

    [ContextMenu("VR 컴포넌트 다시 찾기")]
    public void TestFindVRComponents()
    {
        FindVRComponents();
    }

    [ContextMenu("컴포넌트 상태 출력")]
    public void TestPrintComponentStatus()
    {
        Debug.Log($"[소백이] 컴포넌트 상태:");
        Debug.Log($"playerCamera: {(playerCamera != null ? playerCamera.name : "null")}");
        Debug.Log($"playerTransform: {(playerTransform != null ? playerTransform.name : "null")}");
        Debug.Log($"leftController: {(leftController != null ? leftController.name : "null")}");
        Debug.Log($"rightController: {(rightController != null ? rightController.name : "null")}");
    }
    #endregion
}

/// <summary>
/// 상호작용 가능한 오브젝트 인터페이스 (선택사항)
/// </summary>
public interface IInteractable
{
    void OnSobaekApproach(); // 소백이가 접근했을 때
    void OnSobaekLeave();    // 소백이가 떠날 때
}
