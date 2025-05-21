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

    // 공개 프로퍼티 추가 - 원본 태우리 접근용
    public Taewoori OriginTaewoori => originTaewoori;

    public void Initialize(TaewooriPoolManager taewooriManager, Taewoori taewoori)
    {
        manager = taewooriManager;
        originTaewoori = taewoori;

        // 피버타임 체크 - GameManager 직접 참조
        isFeverMode = GameManager.Instance != null &&
                      GameManager.Instance.CurrentPhase == GameManager.GamePhase.Burning;

        // 피버타임에 따른 체력 설정
        if (isFeverMode)
        {
            maxHealth = 100f + feverTimeExtraHealth;
        }
        else
        {
            maxHealth = 100f;
        }

        ResetState();

        Debug.Log($"작은 태우리 초기화: {gameObject.name}, 원본: {originTaewoori?.name}, 피버모드: {isFeverMode}");
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

        Debug.Log($"작은 태우리 사망: {gameObject.name}, 원본: {originTaewoori?.name}");

        // 풀로 반환 (TaewooriPoolManager에서 발사체 카운트 자동 처리)
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
