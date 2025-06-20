using Photon.Pun;

/// <summary>
/// 스몰태우리 클래스 - NetworkTaewoori를 상속받아 네트워크 기능 사용
/// 파이어파티클이 충돌하여 생성되는 작은 적 유닛
/// 원본 태우리와 연결되어 개수 제한이 관리됨
/// </summary>
public class SmallTaewoori : NetworkTaewoori
{
    #region 변수 선언
    private Taewoori originTaewoori;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 이 스몰태우리를 생성한 원본 태우리 참조
    /// </summary>
    public Taewoori OriginTaewoori => originTaewoori;
    #endregion

    #region 초기화 메서드들
    /// <summary>
    /// 마스터용 초기화 (카운트 증가 포함) - 직접 생성 시 사용
    /// </summary>
    /// <param name="taewooriManager">풀 매니저 참조</param>
    /// <param name="taewoori">원본 태우리 참조</param>
    /// <param name="id">네트워크 고유 ID</param>
    public void Initialize(TaewooriPoolManager taewooriManager, Taewoori taewoori, int id)
    {
        SetupNetwork(taewooriManager, id, false);
        originTaewoori = taewoori;

        // 마스터만 카운트 증가
        if (PhotonNetwork.IsMasterClient && manager != null && originTaewoori != null)
        {
            manager.IncrementSmallTaewooriCount(originTaewoori);
        }

        InitializeHealth();
        ResetState();
    }

    /// <summary>
    /// 마스터용 초기화 (카운트 증가 없이) - 파이어파티클에서 생성할 때 사용
    /// </summary>
    /// <param name="taewooriManager">풀 매니저 참조</param>
    /// <param name="taewoori">원본 태우리 참조</param>
    /// <param name="id">네트워크 고유 ID</param>
    public void InitializeWithoutCountIncrement(TaewooriPoolManager taewooriManager, Taewoori taewoori, int id)
    {
        SetupNetwork(taewooriManager, id, false);
        originTaewoori = taewoori;

        InitializeHealth();
        ResetState();
    }

    /// <summary>
    /// 클라이언트용 초기화 - 시각적 표시만을 위한 스몰태우리로 초기화
    /// </summary>
    /// <param name="taewoori">원본 태우리 참조</param>
    /// <param name="id">네트워크 고유 ID</param>
    public void InitializeAsClient(Taewoori taewoori, int id)
    {
        SetupNetwork(null, id, true);
        originTaewoori = taewoori;

        InitializeHealth();
        ResetState();
    }
    #endregion

    #region 네트워크 동기화 구현
    /// <summary>
    /// 체력 동기화를 위한 네트워크 전송
    /// </summary>
    protected override void SyncHealthToNetwork()
    {
        if (manager != null && networkID != -1)
        {
            ((TaewooriPoolManager)manager).SyncSmallTaewooriDamage(networkID, currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// 클라이언트용 체력 동기화 - 마스터에서 받은 체력 정보 업데이트
    /// </summary>
    /// <param name="newCurrentHealth">새로운 현재 체력</param>
    /// <param name="newMaxHealth">새로운 최대 체력</param>
    public override void SyncHealthFromNetwork(float newCurrentHealth, float newMaxHealth)
    {
        if (PhotonNetwork.IsMasterClient || !isClientOnly)
            return;

        currentHealth = newCurrentHealth;
        maxHealth = newMaxHealth;
    }

    /// <summary>
    /// 클라이언트 데미지 요청 함수 - 마스터면 직접 처리, 클라이언트면 RPC 요청
    /// </summary>
    /// <param name="damage">요청할 데미지량</param>
    public override void RequestDamageFromClient(float damage)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            TakeDamage(damage);
        }
        else
        {
            // 클라이언트면 마스터에게 요청
            if (TaewooriPoolManager.Instance != null && networkID != -1)
            {
                TaewooriPoolManager.Instance.photonView.RPC("RequestSmallTaewooriDamage",
                    RpcTarget.MasterClient, networkID, damage, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
    }
    #endregion

    #region 사망 처리 (NetworkTaewoori 추상 메서드 구현)
    /// <summary>
    /// 스몰태우리 사망 처리 - 마스터는 카운트 감소 및 네트워크 동기화, 클라이언트는 풀 반환만
    /// </summary>
    public override void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 마스터만 카운트 감소 및 네트워크 동기화
        if (PhotonNetwork.IsMasterClient && !isClientOnly)
        {
            if (manager != null && originTaewoori != null)
            {
                manager.DecrementSmallTaewooriCount(originTaewoori);
            }

            // 네트워크로 파괴 알림
            if (manager != null && networkID != -1)
            {
                ((TaewooriPoolManager)manager).SyncSmallTaewooriDestroy(networkID);
            }
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

    /// <summary>
    /// 클라이언트용 사망 처리 - 네트워크에서 호출되는 시각적 파괴만 처리
    /// </summary>
    public void DieAsClient()
    {
        if (PhotonNetwork.IsMasterClient || !isClientOnly)
            return;

        isDead = true;

        // 클라이언트는 풀로만 반환
        if (manager != null)
        {
            manager.ReturnSmallTaewooriToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
}
