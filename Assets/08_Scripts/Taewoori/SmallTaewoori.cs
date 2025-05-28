using UnityEngine;
using Photon.Pun;

/// <summary>
/// 스몰태우리 클래스 - 파이어파티클이 충돌하여 생성되는 작은 적 유닛
/// 원본 태우리와 연결되어 개수 제한이 관리됨
/// </summary>
public class SmallTaewoori : BaseTaewoori
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
        manager = taewooriManager;
        originTaewoori = taewoori;
        networkID = id;
        isClientOnly = false;

        // 마스터만 카운트 증가
        if (PhotonNetwork.IsMasterClient && manager != null && originTaewoori != null)
        {
            manager.IncrementSmallTaewooriCount(originTaewoori);
        }

        InitializeHealth();
        ResetState();

        Debug.Log($"[마스터] 스몰태우리 {networkID} 생성 완료");
    }

    /// <summary>
    /// 마스터용 초기화 (카운트 증가 없이) - 파이어파티클에서 생성할 때 사용
    /// </summary>
    /// <param name="taewooriManager">풀 매니저 참조</param>
    /// <param name="taewoori">원본 태우리 참조</param>
    /// <param name="id">네트워크 고유 ID</param>
    public void InitializeWithoutCountIncrement(TaewooriPoolManager taewooriManager, Taewoori taewoori, int id)
    {
        manager = taewooriManager;
        originTaewoori = taewoori;
        networkID = id;
        isClientOnly = false;

        InitializeHealth();
        ResetState();

        Debug.Log($"[마스터] 스몰태우리 {networkID} 생성 완료 (카운트 증가 없음)");
    }

    /// <summary>
    /// 클라이언트용 초기화 - 시각적 표시만을 위한 스몰태우리로 초기화
    /// </summary>
    /// <param name="taewoori">원본 태우리 참조</param>
    /// <param name="id">네트워크 고유 ID</param>
    public void InitializeAsClient(Taewoori taewoori, int id)
    {
        originTaewoori = taewoori;
        networkID = id;
        isClientOnly = true;

        InitializeHealth();
        ResetState();

        Debug.Log($"[클라이언트] 스몰태우리 {networkID} 시각적 생성 완료");
    }
    #endregion

    #region 네트워크 데미지 시스템
    /// <summary>
    /// 마스터용 데미지 처리 - 실제 데미지 적용 후 네트워크 동기화
    /// </summary>
    /// <param name="damage">적용할 데미지량</param>
    public override void TakeDamage(float damage)
    {
        if (!PhotonNetwork.IsMasterClient || isClientOnly)
            return;

        Debug.Log($"[마스터] 스몰태우리 {networkID} 데미지: {damage}, 체력: {currentHealth}/{maxHealth}");

        base.TakeDamage(damage);

        // 네트워크로 체력 동기화
        if (manager != null && networkID != -1)
        {
            ((TaewooriPoolManager)manager).SyncSmallTaewooriDamage(networkID, currentHealth, maxHealth);
            Debug.Log($"[마스터] 스몰태우리 {networkID} 체력 동기화 전송: {currentHealth}/{maxHealth}");
        }
    }

    /// <summary>
    /// 클라이언트용 체력 동기화 - 마스터에서 받은 체력 정보로 색상 업데이트
    /// </summary>
    /// <param name="newCurrentHealth">새로운 현재 체력</param>
    /// <param name="newMaxHealth">새로운 최대 체력</param>
    public void SyncHealthFromNetwork(float newCurrentHealth, float newMaxHealth)
    {
        if (PhotonNetwork.IsMasterClient || !isClientOnly)
            return;

        Debug.Log($"[클라이언트] 스몰태우리 {networkID} 체력 동기화 받음: {newCurrentHealth}/{newMaxHealth}");

        currentHealth = newCurrentHealth;
        maxHealth = newMaxHealth;

        // 색상 업데이트 (BaseTaewoori의 public 메서드)
        UpdateHealthColor();

        Debug.Log($"[클라이언트] 스몰태우리 {networkID} 색상 업데이트 완료");
    }

    /// <summary>
    /// 클라이언트 데미지 요청 함수 - 마스터면 직접 처리, 클라이언트면 RPC 요청
    /// </summary>
    /// <param name="damage">요청할 데미지량</param>
    public void RequestDamageFromClient(float damage)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 마스터면 직접 처리
            TakeDamage(damage);
        }
        else
        {
            // 클라이언트면 마스터에게 요청
            if (TaewooriPoolManager.Instance != null && networkID != -1)
            {
                Debug.Log($"[클라이언트] 스몰태우리 {networkID}에게 데미지 {damage} 요청 전송");
                TaewooriPoolManager.Instance.photonView.RPC("RequestSmallTaewooriDamage",
                    RpcTarget.MasterClient, networkID, damage, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
    }
    #endregion

    #region 사망 처리
    /// <summary>
    /// 스몰태우리 사망 처리 - 마스터는 카운트 감소 및 네트워크 동기화, 클라이언트는 풀 반환만
    /// </summary>
    public override void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log($"[{(PhotonNetwork.IsMasterClient ? "마스터" : "클라이언트")}] 스몰태우리 {networkID} 사망");

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
                Debug.Log($"[마스터] 스몰태우리 {networkID} 파괴 동기화 전송");
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

        Debug.Log($"[클라이언트] 스몰태우리 {networkID} 네트워크 파괴 받음");

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
