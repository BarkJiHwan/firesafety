using System.Collections;
using Photon.Pun.Demo.PunBasics;
using UnityEngine;

/// <summary>
/// VR 화재 대피 게임용 불 캐릭터 - BaseTaewoori를 상속받아 플레이어 추적 기능 제공
/// 플레이어를 따라다니며 물총에 맞으면 사라지는 어린이 친화적 캐릭터
/// </summary>
public class ExitTaewoori : BaseTaewoori
{
    #region 인스펙터 설정
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float floatingSpeed = 1f;
    [SerializeField] private float floatingHeight = 0.5f;

    [Header("플레이어 추적")]
    [SerializeField] private bool chasePlayer = true;
    [SerializeField] private float chaseDistance = 10f; // 플레이어 추적 거리
    [SerializeField] private float stopDistance = 2f;   // 플레이어와 멈출 거리
    [SerializeField] private string playerTag = "Player"; // 플레이어 태그
    #endregion

    #region 변수 선언
    private Vector3 startPosition;
    private Coroutine movementCoroutine;
    private ExitTaewooriSpawner spawner;
    private Transform targetPlayer;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 이 캐릭터를 생성한 스포너 참조
    /// </summary>
    public ExitTaewooriSpawner Spawner => spawner;
    #endregion

    #region 유니티 라이프사이클
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartMovement();
    }

    private void OnDisable()
    {
        StopMovement();
    }
    #endregion

    #region 초기화
    /// <summary>
    /// ExitTaewoori 초기화 - 스포너 참조와 기본 설정
    /// </summary>
    /// <param name="exitSpawner">이 캐릭터를 생성한 스포너</param>
    public void Initialize(ExitTaewooriSpawner exitSpawner)
    {
        spawner = exitSpawner;
        startPosition = transform.position;

        // 플레이어 찾기
        FindPlayer();

        // BaseTaewoori의 체력 초기화 호출
        InitializeHealth();
        ResetState();

        Debug.Log($"ExitTaewoori 초기화 완료: {gameObject.name}");
    }

    /// <summary>
    /// 플레이어 찾기 (태그 기반)
    /// </summary>
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            targetPlayer = playerObj.transform;
            Debug.Log($"플레이어 발견: {targetPlayer.name}");
        }
        else
        {
            Debug.LogWarning($"'{playerTag}' 태그를 가진 플레이어를 찾을 수 없습니다!");
            targetPlayer = null;
        }
    }
    #endregion

    #region 이동 시스템
    /// <summary>
    /// 이동 시작 - 플레이어 추적 또는 둥둥 떠다니기
    /// </summary>
    private void StartMovement()
    {
        StopMovement(); // 기존 코루틴 정리

        if (chasePlayer)
        {
            movementCoroutine = StartCoroutine(ChasePlayer());
        }
        else
        {
            movementCoroutine = StartCoroutine(FloatingMovement());
        }
    }

    /// <summary>
    /// 이동 중지
    /// </summary>
    private void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
    }

    /// <summary>
    /// 플레이어 추적
    /// </summary>
    private IEnumerator ChasePlayer()
    {
        while (!isDead)
        {
            // 플레이어가 없으면 다시 찾기
            if (targetPlayer == null)
            {
                FindPlayer();
                if (targetPlayer == null)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }
            }

            Vector3 playerPosition = targetPlayer.position;
            Vector3 currentPosition = transform.position;

            // 플레이어와의 거리 계산 (Y축 제외한 수평 거리)
            Vector3 horizontalPlayerPos = new Vector3(playerPosition.x, currentPosition.y, playerPosition.z);
            float distanceToPlayer = Vector3.Distance(currentPosition, horizontalPlayerPos);

            Debug.Log($"{gameObject.name} - 플레이어 거리: {distanceToPlayer:F1}m, 추적거리: {chaseDistance}m");

            // 추적 거리 안에 있고, 멈출 거리보다 멀리 있으면 이동
            if (distanceToPlayer <= chaseDistance && distanceToPlayer > stopDistance)
            {
                // 플레이어 방향으로 이동 (수평 이동만)
                Vector3 direction = (horizontalPlayerPos - currentPosition).normalized;
                Vector3 newPosition = currentPosition + direction * moveSpeed * Time.deltaTime;

                // Y축은 둥둥 떠다니는 효과 적용
                newPosition.y = startPosition.y + Mathf.Sin(Time.time * floatingSpeed) * floatingHeight;

                transform.position = newPosition;

                // 플레이어를 바라보기 (수평으로만)
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }

                Debug.Log($"{gameObject.name} - 플레이어 추적 중!");
            }
            else
            {
                // 추적 거리 밖이거나 너무 가까우면 제자리에서 둥둥
                float newY = startPosition.y + Mathf.Sin(Time.time * floatingSpeed) * floatingHeight;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);

                // 플레이어가 가까이 있으면 계속 바라보기
                if (distanceToPlayer <= chaseDistance)
                {
                    Vector3 lookDirection = (horizontalPlayerPos - transform.position).normalized;
                    if (lookDirection != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(lookDirection);
                    }
                }

                if (distanceToPlayer > chaseDistance)
                {
                    Debug.Log($"{gameObject.name} - 플레이어 추적 거리 밖");
                }
                else
                {
                    Debug.Log($"{gameObject.name} - 플레이어에게 너무 가까움");
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// 제자리에서 둥둥 떠다니는 움직임
    /// </summary>
    private IEnumerator FloatingMovement()
    {
        while (!isDead)
        {
            // 위아래로 둥둥
            float newY = startPosition.y + Mathf.Sin(Time.time * floatingSpeed) * floatingHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // 좌우로 살짝 회전 (선택사항)
            float rotationY = Mathf.Sin(Time.time * floatingSpeed * 0.5f) * 15f;
            transform.rotation = Quaternion.Euler(0, rotationY, 0);

            yield return null;
        }
    }
    #endregion

    #region 데미지 시스템
    /// <summary>
    /// 데미지 처리 - BaseTaewoori의 시스템 활용
    /// </summary>
    /// <param name="damage">적용할 데미지량</param>
    public override void TakeDamage(float damage)
    {
        if (isDead)
            return;

        // BaseTaewoori의 데미지 처리 (체력 감소 + 색상 변화)
        base.TakeDamage(damage);

        Debug.Log($"{gameObject.name} 피격! 데미지: {damage}, 남은 체력: {currentHealth}/{maxHealth}");
    }
    #endregion

    #region 사망 처리
    /// <summary>
    /// ExitTaewoori 사망 처리 - 점수 추가, 이펙트 재생, 스포너에 알림
    /// </summary>
    public override void Die()
    {
        if (isDead)
            return;

        isDead = true;
        StopMovement();

        // 스포너에 사망 알림
        if (spawner != null)
        {
            spawner.OnTaewooriDestroyed(this);
        }

        Debug.Log($"{gameObject.name} 사망!");

        // 오브젝트 제거 또는 풀 반환
        StartCoroutine(DestroyAfterDelay(0.5f)); // 이펙트 재생 시간 고려
    }

    /// <summary>
    /// 지연 후 오브젝트 제거
    /// </summary>
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (spawner != null)
        {
            spawner.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region 퍼블릭 메서드
    /// <summary>
    /// 플레이어 추적 모드 설정
    /// </summary>
    /// <param name="shouldChase">플레이어를 추적할지 여부</param>
    public void SetChasePlayer(bool shouldChase)
    {
        chasePlayer = shouldChase;

        // 이동 모드 변경 시 재시작
        if (gameObject.activeInHierarchy)
        {
            StartMovement();
        }
    }

    /// <summary>
    /// 추적 거리 설정
    /// </summary>
    /// <param name="distance">추적할 거리</param>
    public void SetChaseDistance(float distance)
    {
        chaseDistance = Mathf.Max(0f, distance);
    }

    /// <summary>
    /// 멈출 거리 설정
    /// </summary>
    /// <param name="distance">플레이어와 멈출 거리</param>
    public void SetStopDistance(float distance)
    {
        stopDistance = Mathf.Max(0f, distance);
    }

    /// <summary>
    /// 즉시 제거 (게임 종료 시 등)
    /// </summary>
    public void ForceDestroy()
    {
        isDead = true;
        StopMovement();

        if (spawner != null)
        {
            spawner.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 현재 체력 비율 반환 (UI 표시용)
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }
    #endregion
}
