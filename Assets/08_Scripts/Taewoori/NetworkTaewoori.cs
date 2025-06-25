using UnityEngine;
using Photon.Pun;

/// <summary>
/// 네트워크 멀티플레이어용 태우리 기본 클래스
/// BaseTaewoori를 상속받아 네트워크 기능을 추가
/// </summary>
public abstract class NetworkTaewoori : BaseTaewoori
{
    #region 인스펙터 설정
    [Header("네트워크 체력 설정")]
    [SerializeField] protected float feverTimeExtraHealth = 50f;
    #endregion

    #region 변수 선언
    protected TaewooriPoolManager manager;
    protected bool isFeverMode = false;

    // 네트워크 관련
    protected int networkID = -1;
    protected bool isClientOnly = false;
    protected int lastAttackerID = -1;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 네트워크 고유 ID
    /// </summary>
    public int NetworkID => networkID;
    
    /// <summary>
    /// 피버타임 상태 확인
    /// </summary>
    protected bool IsFeverTime => GameManager.Instance != null &&
                                GameManager.Instance.CurrentPhase == GamePhase.Fever;
    #endregion

    #region 체력 시스템 오버라이드
    /// <summary>
    /// 네트워크용 체력 초기화 - 피버타임 여부에 따라 최대 체력 설정
    /// </summary>
    protected override void InitializeHealth()
    {
        isFeverMode = IsFeverTime;
        maxHealth = isFeverMode ? 100f + feverTimeExtraHealth : 100f;
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 네트워크용 데미지 처리 - 마스터 클라이언트만 실제 데미지 적용
    /// </summary>
    /// <param name="damage">적용할 데미지량</param>
    public override void TakeDamage(float damage)
    {
        if (!PhotonNetwork.IsMasterClient || isClientOnly || isDead)
            return;

        base.TakeDamage(damage);        // BaseTaewoori 호출

        // 히트 애니메이션 RPC
        if (manager != null && networkID != -1 && !isDead)
        {
            SyncHitAnimationToNetwork();
        }

        // 체력 동기화
        if (!isDead)
        {
            SyncHealthToNetwork();
        }
    }
    #endregion

    #region 네트워크 동기화
    /// <summary>
    /// 히트 애니메이션 네트워크 동기화 - 하위 클래스에서 구현
    /// </summary>
    protected virtual void SyncHitAnimationToNetwork()
    {
        // 하위 클래스에서 구현
    }
    /// <summary>
    /// 클라이언트용 체력 동기화 - 하위 클래스에서 구현
    /// </summary>
    /// <param name="newCurrentHealth">새로운 현재 체력</param>
    /// <param name="newMaxHealth">새로운 최대 체력</param>
    public virtual void SyncHealthFromNetwork(float newCurrentHealth, float newMaxHealth)
    {
        if (PhotonNetwork.IsMasterClient || !isClientOnly)
            return;

        currentHealth = newCurrentHealth;
        maxHealth = newMaxHealth;
    }

    /// <summary>
    /// 클라이언트 데미지 요청 함수 - 하위 클래스에서 구현
    /// </summary>
    /// <param name="damage">요청할 데미지량</param>
    public virtual void RequestDamageFromClient(float damage)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            TakeDamage(damage);
        }
        // 클라이언트에서 마스터로 RPC 요청은 하위 클래스에서 구현
    }

    /// <summary>
    /// 체력 동기화를 위한 네트워크 전송 - 하위 클래스에서 구현
    /// </summary>
    protected abstract void SyncHealthToNetwork();
    #endregion

    #region 초기화
    /// <summary>
    /// 네트워크 매니저 설정
    /// </summary>
    /// <param name="poolManager">풀 매니저 참조</param>
    /// <param name="id">네트워크 ID</param>
    /// <param name="clientOnly">클라이언트 전용 여부</param>
    protected virtual void SetupNetwork(TaewooriPoolManager poolManager, int id, bool clientOnly = false)
    {
        manager = poolManager;
        networkID = id;
        isClientOnly = clientOnly;
    }
    // NetworkTaewoori.cs에 추가
    public override void Die()
    {
        if (isDead)
            return;

        base.Die();        // BaseTaewoori의 애니메이션 로직

        if (PhotonNetwork.IsMasterClient && !isClientOnly)
        {
            //사망 애니메이션 RPC
            if (manager != null && networkID != -1)
            {
                SyncDeathAnimationToNetwork();
            }

            // 하위 클래스별 사망 로직
            HandleMasterDeathLogic();
        }
    }

    /// <summary>
    /// 사망 애니메이션 네트워크 동기화 - 하위 클래스에서 구현
    /// </summary>
    protected virtual void SyncDeathAnimationToNetwork()
    {
        // 하위 클래스에서 구현
    }

    /// <summary>
    /// 마스터 사망 로직 - 하위 클래스에서 구현
    /// </summary>
    protected virtual void HandleMasterDeathLogic()
    {
        // 하위 클래스에서 구현
    }
    #endregion

    #region 어떤플레이어가 태우리 처치했는지
    public void SetLastAttacker(int attackerID)
    {
        lastAttackerID = attackerID;
    }
    public int GetLastAttackerID()
    {
        return lastAttackerID;
    }


    #endregion
}
