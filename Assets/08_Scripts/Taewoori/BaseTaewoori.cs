using UnityEngine;

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
    #endregion

    #region 변수 선언
    protected bool isDead = false;
    protected Animator animator;

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
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
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

        // 애니메이션 상태 리셋
        if (animator != null)
        {
            animator.SetBool(hashHit, false);
        }
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

        // Hit 애니메이션 재생
        PlayHitAnimation();

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
        if (animator != null)
        {
            animator.SetBool(hashHit, true);
            // 짧은 시간 후 Hit 상태를 false로 되돌림
            Invoke(nameof(ResetHitAnimation), 0.1f);
        }
    }

    /// <summary>
    /// Hit 애니메이션 리셋
    /// </summary>
    protected virtual void ResetHitAnimation()
    {
        if (animator != null)
        {
            animator.SetBool(hashHit, false);
        }
    }

    /// <summary>
    /// 죽는 애니메이션 재생
    /// </summary>
    protected virtual void PlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(hashIsDead);
        }
    }
    #endregion

    #region 추상 메서드
    /// <summary>
    /// 사망 처리 - 상속받은 클래스에서 구체적인 사망 로직 구현
    /// </summary>
    public virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;
        PlayDeathAnimation();
    }
    #endregion
}
