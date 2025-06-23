using UnityEngine;
using System.Collections;

/// <summary>
/// 모든 태우리의 기본 클래스 - 순수 체력 관리 기능만 제공
/// IDamageable 인터페이스를 구현하여 데미지 시스템과 연동
/// </summary>
public abstract class BaseTaewoori : MonoBehaviour, IDamageable
{
    #region 인스펙터 설정
    [Header("체력 설정")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;

    [Header("애니메이션 설정")]
    [SerializeField] protected bool useAnimation = true; // 애니메이션 사용 여부
    #endregion

    #region 변수 선언
    protected bool isDead = false;
    protected Animator animator;
    protected bool hasAnimator = false; // Animator 존재 여부

    // 애니메이션 해시
    private readonly int hashHit = Animator.StringToHash("Hit");
    private readonly int hashIsDead = Animator.StringToHash("IsDead");
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 사망 상태 확인
    /// </summary>
    public bool IsDead => isDead;

    /// <summary>
    /// 현재 체력
    /// </summary>
    public float CurrentHealth => currentHealth;

    /// <summary>
    /// 최대 체력
    /// </summary>
    public float MaxHealth => maxHealth;

    /// <summary>
    /// 애니메이션 사용 여부
    /// </summary>
    public bool UseAnimation => useAnimation && hasAnimator;
    #endregion

    #region 유니티 라이프사이클
    protected virtual void Awake()
    {
        InitializeComponents();
        InitializeHealth();
    }

    protected virtual void OnEnable()
    {
        ResetState();
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    protected virtual void InitializeComponents()
    {
        // Animator 컴포넌트 찾기
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Animator 존재 여부 확인
        hasAnimator = animator != null;

        // 애니메이션을 사용하지 않는 오브젝트인 경우 로그 출력 안함
        if (useAnimation && !hasAnimator)
        {
            Debug.LogWarning($"{gameObject.name}: 애니메이션이 설정되어 있지만 Animator가 없습니다. " +
                           "파티클 기반 오브젝트라면 useAnimation을 false로 설정하세요.");
        }
    }

    /// <summary>
    /// 애니메이션 사용 설정 변경 (런타임에서 호출 가능)
    /// </summary>
    /// <param name="use">애니메이션 사용 여부</param>
    protected void SetUseAnimation(bool use)
    {
        useAnimation = use;
    }
    #endregion

    #region 체력 시스템
    /// <summary>
    /// 체력 초기화
    /// </summary>
    protected virtual void InitializeHealth()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 상태 리셋 - 체력과 생존 상태 초기화
    /// </summary>
    protected virtual void ResetState()
    {
        currentHealth = maxHealth;
        isDead = false;
        // 트리거는 자동 리셋되므로 별도 처리 불필요
    }

    /// <summary>
    /// IDamageable 인터페이스 구현 - 데미지 적용 및 체력 감소 처리
    /// </summary>
    /// <param name="damage">적용할 데미지량</param>
    public virtual void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        // Hit 애니메이션 재생 (애니메이션을 사용하는 경우만)
        if (UseAnimation)
        {
            PlayHitAnimation();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    #endregion

    #region 애니메이션
    /// <summary>
    /// 맞는 애니메이션 재생
    /// </summary>
    protected virtual void PlayHitAnimation()
    {
        if (UseAnimation)
        {
            animator.SetTrigger(hashHit);
        }
    }

    /// <summary>
    /// 죽는 애니메이션 재생
    /// </summary>
    protected virtual void PlayDeathAnimation()
    {
        if (UseAnimation)
        {
            animator.SetTrigger(hashIsDead);
        }
    }
    #endregion

    #region 사망 처리
    /// <summary>
    /// 사망 처리 - 상속받은 클래스에서 구체적인 사망 로직 구현
    /// </summary>
    public virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 애니메이션을 사용하는 경우
        if (UseAnimation)
        {
            PlayDeathAnimation();
            // Death 애니메이션 완료 후 자동으로 오브젝트 처리
            StartCoroutine(HandleDeathSequence());
        }
        else
        {
            // 애니메이션이 없는 경우 즉시 처리
            PerformFinalDeath();
        }
    }

    /// <summary>
    /// Death 애니메이션 완료 후 오브젝트 처리
    /// </summary>
    protected virtual System.Collections.IEnumerator HandleDeathSequence()
    {
        // Death 애니메이션 길이만큼 대기
        yield return new WaitForSeconds(3f);

        // 오브젝트 비활성화 또는 파괴
        PerformFinalDeath();
    }

    /// <summary>
    /// 최종 사망 처리 - 오브젝트 비활성화 또는 파괴
    /// </summary>
    protected virtual void PerformFinalDeath()
    {
        // 기본적으로 오브젝트 비활성화
        gameObject.SetActive(false);
    }
    #endregion
}
