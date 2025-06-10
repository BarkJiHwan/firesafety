using System;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 주 태우리 클래스 - 마스터 클라이언트가 로직을 담당하고 클라이언트들은 시각적 동기화만 처리
/// 발사체를 생성하여 스몰태우리를 만들어내는 주요 적 유닛
/// </summary>
public class Taewoori : BaseTaewoori
{
    #region 이벤트 선언
    /// <summary>
    /// 태우리 생성 시 발생하는 이벤트
    /// </summary>
    public static event Action<Taewoori, FireObjScript> OnTaewooriSpawned;

    /// <summary>
    /// 태우리 파괴 시 발생하는 이벤트
    /// </summary>
    public static event Action<Taewoori, FireObjScript> OnTaewooriDestroyed;
    #endregion

    #region 인스펙터 설정
    [Header("포물선 발사 설정")]
    [SerializeField] private float throwForce = 3f;
    [SerializeField] private float upwardForce = 2f;
    [SerializeField] private float fireRadius = 2f;
    [SerializeField] private bool useRandomDirection = true;

    [Header("발사체 설정")]
    [SerializeField] private int maxSmallTaewooriCount = 4;
    [SerializeField] private float projectileCooldown = 2.0f;
    [SerializeField] private float spawnHeight = 0.5f;
    #endregion

    #region 변수 선언
    private bool _isDead = false;
    public new bool IsDead => _isDead;

    private float _coolTime;
    private FireObjScript sourceFireObj;

    // 프로퍼티
    public int MaxSmallTaewooriCount => maxSmallTaewooriCount;
    public FireObjScript SourceFireObj => sourceFireObj;
    #endregion

    #region 초기화 메서드들
    /// <summary>
    /// 마스터 클라이언트용 초기화 - 실제 게임 로직을 처리하는 태우리로 초기화
    /// </summary>
    /// <param name="taewooriManager">풀 매니저 참조</param>
    /// <param name="fireObj">연결된 화재 오브젝트</param>
    /// <param name="id">네트워크 고유 ID</param>
    public void Initialize(TaewooriPoolManager taewooriManager, FireObjScript fireObj, int id)
    {
        manager = taewooriManager;
        sourceFireObj = fireObj;
        networkID = id;
        isClientOnly = false;

        if (sourceFireObj != null)
        {
            sourceFireObj.SetActiveTaewoori(this);
        }

        InitializeHealth();
        ResetState();
        OnTaewooriSpawned?.Invoke(this, sourceFireObj);
    }

    /// <summary>
    /// 클라이언트용 초기화 - 시각적 표시만을 위한 태우리로 초기화
    /// </summary>
    /// <param name="id">네트워크 고유 ID</param>
    public void InitializeAsClient(int id)
    {
        networkID = id;
        isClientOnly = true;
        sourceFireObj = null;

        InitializeHealth();
        ResetState();
    }

    /// <summary>
    /// 태우리 상태 리셋 - 체력, 쿨타임, 생존 상태 초기화
    /// </summary>
    protected override void ResetState()
    {
        base.ResetState();
        _coolTime = 0f;
        _isDead = false;
    }
    #endregion

    #region 게임 로직 (마스터 전용)
    /// <summary>
    /// 매 프레임 업데이트 - 마스터 클라이언트만 실행
    /// 발사체 쿨타임 관리 및 발사 로직 처리
    /// </summary>
    public void Update()
    {
        // 마스터 클라이언트만 로직 실행
        if (!PhotonNetwork.IsMasterClient || isClientOnly)
            return;

        if (sourceFireObj == null)
            return;

        _coolTime += Time.deltaTime;

        // 발사체 로직
        if (manager != null && projectileCooldown <= _coolTime &&
            manager.CanLaunchProjectile(this, maxSmallTaewooriCount))
        {
            LaunchProjectile();
        }
    }

    /// <summary>
    /// 발사체 생성 및 발사 - 포물선 궤도로 파이어파티클 발사
    /// </summary>
    private void LaunchProjectile()
    {
        if (!PhotonNetwork.IsMasterClient || isClientOnly)
            return;

        Vector3 launchPosition = transform.position + Vector3.up * spawnHeight;
        Vector3 targetPosition = CalculateCircularTarget();
        Vector3 horizontalDirection = (targetPosition - transform.position).normalized;
        Vector3 launchVelocity = horizontalDirection * throwForce + Vector3.up * upwardForce;

        if (manager != null)
        {
            Quaternion launchRotation = Quaternion.LookRotation(launchVelocity.normalized);
            GameObject projectile = manager.PoolSpawnFireParticle(launchPosition, launchRotation, this);

            if (projectile != null)
            {
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.AddForce(launchVelocity, ForceMode.Impulse);
                    _coolTime = 0f;
                }
            }
        }
    }

    /// <summary>
    /// 발사체 목표 지점 계산 - 랜덤 또는 원형 패턴으로 타겟 설정
    /// </summary>
    /// <returns>계산된 목표 위치</returns>
    private Vector3 CalculateCircularTarget()
    {
        if (useRandomDirection)
        {
            float randomAngle = UnityEngine.Random.Range(0f, 360f);
            Vector3 direction = new Vector3(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(randomAngle * Mathf.Deg2Rad)
            );
            return transform.position + direction * fireRadius;
        }
        else
        {
            int directions = 8;
            float angleStep = 360f / directions;
            float currentAngle = (Time.time * 50f) % 360f;

            Vector3 direction = new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            );
            return transform.position + direction * fireRadius;
        }
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

        base.TakeDamage(damage);

        // 네트워크로 체력 동기화
        if (manager != null && networkID != -1)
        {
            ((TaewooriPoolManager)manager).SyncTaewooriDamage(networkID, currentHealth, maxHealth);
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

        currentHealth = newCurrentHealth;
        maxHealth = newMaxHealth;

        // 색상 업데이트 (BaseTaewoori의 public 메서드)
        UpdateHealthColor();
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
                TaewooriPoolManager.Instance.photonView.RPC("RequestTaewooriDamage",
                    RpcTarget.MasterClient, networkID, damage, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
    }
    #endregion

    #region 사망 처리
    /// <summary>
    /// 태우리 사망 처리 - 생존시간 기록, 처치 점수, 네트워크 동기화, 리스폰 처리
    /// </summary>
    public override void Die()
    {
        if (_isDead) // isDead 대신 _isDead 사용
            return;

        _isDead = true; // _isDead 사용

        Debug.Log($"[{(PhotonNetwork.IsMasterClient ? "마스터" : "클라이언트")}] 태우리 {networkID} 사망");

        // 마스터만 실제 로직 처리
        if (PhotonNetwork.IsMasterClient && !isClientOnly)
        {
            // 1. 처치자 ID 가져오기
            int killerID = GetLastAttackerID();

            // 2. 생존시간 및 처치 기록 (매니저에 직접 기록)
            if (manager != null && killerID != -1)
            {
                ((TaewooriPoolManager)manager).UpdateSurvivalTimeAndRecordKill(networkID, killerID);
                Debug.Log($"[마스터] 태우리 {networkID} 처치자 {killerID} 기록 완료");
            }
            else if (killerID == -1)
            {
                Debug.LogWarning($"[마스터] 태우리 {networkID} 처치자 정보 없음!");
            }

            // 3. 네트워크로 파괴 알림
            if (manager != null)
            {
                ((TaewooriPoolManager)manager).SyncTaewooriDestroy(networkID);
                Debug.Log($"[마스터] 태우리 {networkID} 파괴 동기화 전송");
            }

            // 4. 이벤트 발생 (리스폰 처리용) - sourceFireObj 사용
            OnTaewooriDestroyed?.Invoke(this, sourceFireObj);
        }

        // 5. 풀로 반환 (마스터/클라이언트 공통)
        if (manager != null)
        {
            manager.ReturnTaewooriToPool(gameObject);
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

        Debug.Log($"[클라이언트] 태우리 {networkID} 네트워크 파괴 받음");

        _isDead = true; // _isDead 사용

        // 클라이언트는 풀로만 반환
        if (manager != null)
        {
            manager.ReturnTaewooriToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 마지막 공격자 ID 가져오기 - BaseTaewoori의 lastAttackerID 사용
    /// </summary>
    private int GetLastAttackerID()
    {
        // BaseTaewoori에 lastAttackerID 변수가 있다면 사용
        // return lastAttackerID;

        // 임시: 랜덤 플레이어 ID 반환 (테스트용)
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 0)
        {
            var players = PhotonNetwork.CurrentRoom.Players;
            var playerArray = new int[players.Count];
            int index = 0;
            foreach (var player in players.Values)
            {
                playerArray[index++] = player.ActorNumber;
            }
            return playerArray[UnityEngine.Random.Range(0, playerArray.Length)];
        }

        Debug.LogWarning("GetLastAttackerID() - 플레이어 정보 없음!");
        return -1;
    }
    #endregion
}
