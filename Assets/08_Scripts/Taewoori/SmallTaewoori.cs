using UnityEngine;

public class SmallTaewoori : MonoBehaviour, IDamageable
{
    [Header("체력 설정")]
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float currentHealth;
    [SerializeField] private float feverTimeExtraHealth = 50f;

    private bool isDead = false;
    private TaewooriPoolManager manager;
    private Taewoori originTaewoori;
    private bool isFeverMode = false;
    public void Initialize(TaewooriPoolManager taewooriManager, Taewoori taewoori)
    {
        manager = taewooriManager;
        originTaewoori = taewoori;

        var spawnManager = FindAnyObjectByType<TaewooriSpawnManager>();
        if (spawnManager != null)
        {
            isFeverMode = spawnManager.IsFeverTime;
            if (isFeverMode)
            {
                maxHealth = 100f + feverTimeExtraHealth;
            }
            else
            {
                maxHealth = 100f;
            }
        }

        ResetState();
    }

    private void OnEnable()
    {
        ResetState();
    }

    private void ResetState()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name}이(가) {damage}의 데미지를 받음. 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 원본 태우리에 알림
        if (manager != null && originTaewoori != null)
        {
            manager.NotifySmallTaewooriDestroyed(originTaewoori);
        }

        // 풀로 반환
        if (manager != null)
        {
            manager.ReturnSmallTaewooriToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
