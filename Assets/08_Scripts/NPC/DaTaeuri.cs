using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 다태우리 보스몹 - 플레이어를 바라보며 둥둥 떠다니는 보스
/// </summary>
public class DaTaeuri : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("타겟 설정")]
    [SerializeField] private Transform playerTransform; // 플레이어 Transform (직접 연결)

    [Header("이동 설정")]
    [SerializeField] private float floatingSpeed = 1f; // 둥둥 효과 속도
    [SerializeField] private float floatingHeight = 0.5f; // 둥둥 효과 높이
    [SerializeField] private float rotationSpeed = 2f; // 회전 속도
    [SerializeField] private float approachSpeed = 1f; // 플레이어 접근 속도
    [SerializeField] private float approachDistance = 5f; // 플레이어에게 다가갈 거리
    #endregion

    #region 변수 선언
    private Vector3 basePosition; // 기준 위치
    private float floatTimer = 0f; // 둥둥 효과용 타이머
    private bool isApproaching = false; // 플레이어에게 접근 중인지
    private float targetApproachDistance = 5f; // 접근할 거리
    private Coroutine approachCoroutine; // 접근 코루틴
    #endregion

    #region 유니티 라이프사이클
    private void Start()
    {
        // 시작 위치를 기준 위치로 설정
        basePosition = transform.position;
    }

    private void Update()
    {
        UpdateFloatingEffect(); // 둥둥 효과
        UpdateRotation(); // 플레이어 바라보기
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
    /// 플레이어 바라보기 회전 처리
    /// </summary>
    private void UpdateRotation()
    {
        if (playerTransform == null)
            return;

        // basePosition 사용하고 Y축 무시하지 않기
        Vector3 lookDirection = (playerTransform.position - basePosition);

        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }
    #endregion

    #region 퍼블릭 메서드
    /// <summary>
    /// 기준 위치 설정
    /// </summary>
    public void SetBasePosition(Vector3 newPosition)
    {
        basePosition = newPosition;
    }

    /// <summary>
    /// 플레이어 타겟 설정
    /// </summary>
    public void SetPlayerTarget(Transform player)
    {
        playerTransform = player;
    }

    /// <summary>
    /// 플레이어에게 접근 시작 (Inspector 설정값 사용)
    /// </summary>
    public void StartApproaching()
    {
        StartApproachingPlayer(approachDistance, approachSpeed);
    }

    /// <summary>
    /// 플레이어에게 접근 시작
    /// </summary>
    public void StartApproachingPlayer(float approachDistance, float moveSpeed)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("플레이어 Transform이 설정되지 않음");
            return;
        }

        targetApproachDistance = approachDistance;
        approachSpeed = moveSpeed;
        isApproaching = true;

        // 기존 접근 코루틴이 있으면 중지
        if (approachCoroutine != null)
        {
            StopCoroutine(approachCoroutine);
        }

        approachCoroutine = StartCoroutine(ApproachPlayer());
        Debug.Log($"플레이어에게 접근 시작 - 목표 거리: {approachDistance}m, 속도: {moveSpeed}");
    }

    /// <summary>
    /// 플레이어 접근 중지
    /// </summary>
    public void StopApproaching()
    {
        isApproaching = false;

        if (approachCoroutine != null)
        {
            StopCoroutine(approachCoroutine);
            approachCoroutine = null;
        }

        Debug.Log("플레이어 접근 중지");
    }

    /// <summary>
    /// 플레이어에게 천천히 접근하는 코루틴
    /// </summary>
    private IEnumerator ApproachPlayer()
    {
        while (isApproaching && playerTransform != null)
        {
            // 플레이어 위치
            Vector3 playerPos = playerTransform.position;
            Vector3 currentPos = basePosition;

            // 플레이어와의 거리 계산
            float distanceToPlayer = Vector3.Distance(currentPos, playerPos);

            Debug.Log($"현재 거리: {distanceToPlayer:F2}m, 목표 거리: {targetApproachDistance}m");

            // 목표 거리에 도달했으면 중지
            if (distanceToPlayer <= targetApproachDistance)
            {
                Debug.Log("목표 거리에 도달 - 접근 완료");
                StopApproaching();
                break;
            }

            // 플레이어 방향으로 이동 (목표 거리만큼 떨어진 지점으로)
            Vector3 directionToPlayer = (playerPos - currentPos).normalized;
            Vector3 targetPosition = playerPos - directionToPlayer * targetApproachDistance;

            // 새로운 기준 위치 계산 (천천히 이동)
            Vector3 newBasePosition = Vector3.MoveTowards(currentPos, targetPosition, approachSpeed * Time.deltaTime);

            // 기준 위치 업데이트
            basePosition = newBasePosition;

            yield return null;
        }
    }

    /// <summary>
    /// 둥둥 효과 속도 설정
    /// </summary>
    public void SetFloatingSpeed(float speed)
    {
        floatingSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// 둥둥 효과 높이 설정
    /// </summary>
    public void SetFloatingHeight(float height)
    {
        floatingHeight = Mathf.Max(0f, height);
    }

    /// <summary>
    /// 회전 속도 설정
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// 플레이어와의 거리 반환
    /// </summary>
    public float GetDistanceToPlayer()
    {
        if (playerTransform == null)
            return float.MaxValue;

        return Vector3.Distance(transform.position, playerTransform.position);
    }
    #endregion
}
