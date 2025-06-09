using UnityEngine;
using Photon.Pun;

/// <summary>
/// 모든 태우리의 기본 클래스 - 체력 관리, 색상 변화, 네트워크 기본 기능 제공
/// IDamageable 인터페이스를 구현하여 데미지 시스템과 연동
/// </summary>
public abstract class BaseTaewoori : MonoBehaviour, IDamageable
{
    #region 인스펙터 설정
    [Header("체력 설정")]
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float currentHealth;
    [SerializeField] protected float feverTimeExtraHealth = 50f;

    [Header("체력별 색상 설정")]
    [SerializeField] private ParticleSystem[] targetParticleSystems;
    [SerializeField] private float maxGreenBoost = 1.0f;
    #endregion

    #region 변수 선언
    // 원본 그라디언트 저장용
    private Gradient[] originalGradients;

    protected bool isDead = false;
    protected TaewooriPoolManager manager;
    protected bool isFeverMode = false;

    // 네트워크 관련
    protected int networkID = -1;
    protected bool isClientOnly = false;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 네트워크 고유 ID
    /// </summary>
    public int NetworkID => networkID;

    /// <summary>
    /// 사망 상태 확인
    /// </summary>
    public bool IsDead => isDead;

    /// <summary>
    /// 피버타임 상태 확인
    /// </summary>
    protected bool IsFeverTime => GameManager.Instance != null &&
                                GameManager.Instance.CurrentPhase == GamePhase.Fever;
    #endregion

    #region 유니티 라이프사이클
    protected virtual void Awake()
    {
        SaveOriginalGradients();
    }

    protected virtual void OnEnable()
    {
        if (originalGradients == null)
        {
            SaveOriginalGradients();
        }

        ResetState();
    }
    #endregion

    #region 체력 시스템
    /// <summary>
    /// 체력 초기화 - 피버타임 여부에 따라 최대 체력 설정
    /// </summary>
    protected void InitializeHealth()
    {
        isFeverMode = IsFeverTime;
        maxHealth = isFeverMode ? 100f + feverTimeExtraHealth : 100f;
        currentHealth = maxHealth;

        if (originalGradients == null)
        {
            SaveOriginalGradients();
        }

        UpdateHealthColor();
    }

    /// <summary>
    /// 상태 리셋 - 체력과 생존 상태 초기화
    /// </summary>
    protected virtual void ResetState()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthColor();
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
        UpdateHealthColor();

        Debug.Log($"{gameObject.name} 데미지: {damage}, 현재 체력: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    #endregion

    #region 색상 시스템
    /// <summary>
    /// 원본 파티클 그라디언트 저장 - 색상 변화 기준점 설정
    /// </summary>
    private void SaveOriginalGradients()
    {
        if (targetParticleSystems == null || targetParticleSystems.Length == 0)
            return;

        originalGradients = new Gradient[targetParticleSystems.Length];

        for (int i = 0; i < targetParticleSystems.Length; i++)
        {
            if (targetParticleSystems[i] != null)
            {
                var colorOverLifetime = targetParticleSystems[i].colorOverLifetime;
                if (colorOverLifetime.enabled)
                {
                    originalGradients[i] = new Gradient();
                    var original = colorOverLifetime.color.gradient;
                    originalGradients[i].SetKeys(original.colorKeys, original.alphaKeys);
                }
            }
        }
    }

    /// <summary>
    /// 체력에 따른 색상 업데이트 - 체력 감소 시 녹색/파란색 증가
    /// </summary>
    public void UpdateHealthColor()
    {
        if (targetParticleSystems != null && targetParticleSystems.Length > 0)
        {
            float healthPercentage = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            UpdateParticleColorValues(healthPercentage);
        }
    }

    /// <summary>
    /// 파티클 시스템 색상 값 업데이트 - 체력 비율에 따라 그라디언트 조정
    /// </summary>
    /// <param name="healthPercentage">현재 체력 비율 (0.0 ~ 1.0)</param>
    private void UpdateParticleColorValues(float healthPercentage)
    {
        if (targetParticleSystems == null || targetParticleSystems.Length == 0 || originalGradients == null)
            return;

        float damageRatio = 1f - healthPercentage;
        float greenBoost = damageRatio * maxGreenBoost;
        float blueBoost = greenBoost * 0.3f;

        for (int psIndex = 0; psIndex < targetParticleSystems.Length; psIndex++)
        {
            ParticleSystem ps = targetParticleSystems[psIndex];

            if (ps == null || originalGradients[psIndex] == null)
                continue;

            var colorOverLifetime = ps.colorOverLifetime;

            Gradient originalGradient = originalGradients[psIndex];
            Gradient newGradient = new Gradient();

            GradientColorKey[] originalColorKeys = originalGradient.colorKeys;
            GradientColorKey[] newColorKeys = new GradientColorKey[originalColorKeys.Length];

            for (int i = 0; i < originalColorKeys.Length; i++)
            {
                Color originalColor = originalColorKeys[i].color;
                float keyTime = originalColorKeys[i].time;
                Color newColor;

                if (i == 0) // 첫 번째 키만 색상 변화 적용
                {
                    newColor = new Color(
                        originalColor.r,
                        Mathf.Clamp01(originalColor.g + greenBoost),
                        Mathf.Clamp01(originalColor.b + blueBoost),
                        originalColor.a
                    );
                }
                else
                {
                    newColor = originalColor;
                }

                newColorKeys[i] = new GradientColorKey(newColor, keyTime);
            }

            GradientAlphaKey[] originalAlphaKeys = originalGradient.alphaKeys;
            newGradient.SetKeys(newColorKeys, originalAlphaKeys);
            colorOverLifetime.color = newGradient;
        }
    }
    #endregion

    #region 네트워크 지원 함수
    /// <summary>
    /// 네트워크용 체력 및 색상 동기화 - 클라이언트에서 마스터 데이터로 업데이트
    /// </summary>
    /// <param name="newCurrentHealth">새로운 현재 체력</param>
    /// <param name="newMaxHealth">새로운 최대 체력</param>
    public void SyncHealthAndColor(float newCurrentHealth, float newMaxHealth)
    {
        currentHealth = newCurrentHealth;
        maxHealth = newMaxHealth;
        UpdateHealthColor();
    }
    #endregion

    #region 추상 메서드
    /// <summary>
    /// 사망 처리 - 상속받은 클래스에서 구체적인 사망 로직 구현
    /// </summary>
    public abstract void Die();
    #endregion
}
