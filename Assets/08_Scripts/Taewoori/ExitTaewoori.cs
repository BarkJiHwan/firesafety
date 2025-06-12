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
    [SerializeField] private float floatingSpeed = 1f;
    [SerializeField] private float floatingHeight = 0.5f;
    #endregion

    #region 변수 선언
    private Vector3 startPosition;
    private Coroutine movementCoroutine;
    private ExitTaewooriSpawner spawner;
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

        // BaseTaewoori의 체력 초기화 호출
        InitializeHealth();
        ResetState();

        Debug.Log($"ExitTaewoori 초기화 완료: {gameObject.name}");
    }
    #endregion

    #region 이동 시스템
    /// <summary>
    /// 이동 시작 - 둥둥 떠다니기
    /// </summary>
    private void StartMovement()
    {
        StopMovement(); // 기존 코루틴 정리
        movementCoroutine = StartCoroutine(FloatingMovement());
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
