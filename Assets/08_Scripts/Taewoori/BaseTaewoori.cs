using UnityEngine;

public abstract class BaseTaewoori : MonoBehaviour, IDamageable
{
    [Header("체력 설정")]
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float currentHealth;
    [SerializeField] protected float feverTimeExtraHealth = 50f;

    protected bool isDead = false;
    protected TaewooriPoolManager manager;
    protected bool isFeverMode = false;

    // 피버타임 체크 속성 (GameManager 활용)
    protected bool IsFeverTime => GameManager.Instance != null &&
                                GameManager.Instance.CurrentPhase == GameManager.GamePhase.Fever;

    // 체력 초기화
    protected void InitializeHealth()
    {
        isFeverMode = IsFeverTime;
        maxHealth = isFeverMode ? 100f + feverTimeExtraHealth : 100f;
        currentHealth = maxHealth;
    }

    // 상태 리셋
    protected virtual void ResetState()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    protected virtual void OnEnable()
    {
        ResetState();
    }

    // IDamageable 구현
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

    // 사망 처리 - 상속받은 클래스에서 구현
    public abstract void Die();
}
