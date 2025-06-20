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
        InitializeHealth();
    }

    protected virtual void OnEnable()
    {
        ResetState();
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

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 체력을 직접 설정하는 메서드 (특수한 경우에만 사용)
    /// </summary>
    /// <param name="newHealth">새로운 체력값</param>
    protected virtual void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    /// <summary>
    /// 최대 체력을 설정하는 메서드
    /// </summary>
    /// <param name="newMaxHealth">새로운 최대 체력값</param>
    protected virtual void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }
    #endregion

    #region 추상 메서드
    /// <summary>
    /// 사망 처리 - 상속받은 클래스에서 구체적인 사망 로직 구현
    /// </summary>
    public abstract void Die();
    #endregion
}
