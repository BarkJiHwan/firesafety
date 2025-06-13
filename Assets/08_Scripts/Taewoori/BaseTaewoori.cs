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

    #endregion

    #region 변수 선언

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

    protected virtual void OnEnable()
    {
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
    }
    #endregion

    #region 추상 메서드
    /// <summary>
    /// 사망 처리 - 상속받은 클래스에서 구체적인 사망 로직 구현
    /// </summary>
    public abstract void Die();
    #endregion
}
