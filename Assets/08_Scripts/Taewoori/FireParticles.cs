using System.Collections;
using UnityEngine;

/// <summary>
/// 파이어파티클 클래스 - 태우리가 발사하는 발사체로 충돌 시 스몰태우리 생성
/// 물리 기반 포물선 궤도로 이동하며 지형이나 Shield와 충돌 처리
/// </summary>
public class FireParticles : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("충돌 설정")]
    [SerializeField] private LayerMask ignoreCollisionLayers; // 무시할 레이어 마스크
    [SerializeField] private string shieldTag = "Shield"; // Shield 태그

    [Header("시간 설정")]
    [SerializeField] private float autoDestroyTime = 5f; // 자동 파괴 시간

    [Header("지면 감지 설정")]
    [SerializeField] private float sphereRadius = 0.2f; // 구체 반지름
    #endregion

    #region 변수 선언
    private Taewoori originTaewoori; // 이 발사체를 생성한 원본 태우리
    private bool hasCollided = false; // 충돌 상태
    private Coroutine autoDestroyCoroutine; // 자동 파괴 코루틴
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 이 발사체를 생성한 원본 태우리 참조
    /// </summary>
    public Taewoori OriginTaewoori => originTaewoori;
    #endregion

    #region 유니티 라이프사이클
    private void Start()
    {
        SetupPhysics();
        SetupCollisionIgnoring();
    }

    private void OnEnable()
    {
        ResetState();
        StartAutoDestroyTimer();
    }

    private void OnDisable()
    {
        StopAutoDestroyTimer();
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 물리 속성 설정
    /// </summary>
    private void SetupPhysics()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }

    /// <summary>
    /// 충돌 무시 설정
    /// </summary>
    private void SetupCollisionIgnoring()
    {
        // 태우리와 물리적 충돌 무시 설정
        if (originTaewoori != null)
        {
            IgnoreCollisionWith(originTaewoori.GetComponent<Collider>());
        }
    }

    /// <summary>
    /// 특정 콜라이더와 충돌 무시
    /// </summary>
    /// <param name="otherCollider">무시할 콜라이더</param>
    private void IgnoreCollisionWith(Collider otherCollider)
    {
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null && otherCollider != null)
        {
            Physics.IgnoreCollision(myCollider, otherCollider);
        }
    }

    /// <summary>
    /// 상태 리셋
    /// </summary>
    private void ResetState()
    {
        hasCollided = false;
    }

    /// <summary>
    /// 원본 태우리 설정 및 충돌 무시 처리
    /// </summary>
    /// <param name="taewoori">이 발사체를 생성한 태우리</param>
    public void SetOriginTaewoori(Taewoori taewoori)
    {
        originTaewoori = taewoori;

        // 원본 태우리와 물리적 충돌 무시
        if (originTaewoori != null)
        {
            IgnoreCollisionWith(originTaewoori.GetComponent<Collider>());
        }
    }
    #endregion

    #region 충돌 처리
    /// <summary>
    /// 트리거 충돌 처리 - Shield, 무시 레이어, 일반 지형에 따른 분기 처리
    /// </summary>
    /// <param name="other">충돌한 콜라이더</param>
    private void OnTriggerEnter(Collider other)
    {
        if (hasCollided)
            return;

        // Shield와 충돌 - 스몰태우리 생성 없이 즉시 제거
        if (IsShield(other))
        {
            HandleShieldCollision();
            return;
        }

        // CHM 추가함: 무시할 레이어와 충돌 - 카운트 감소하고 제거
        if (ShouldIgnoreCollision(other))
        {
            HandleIgnoreLayerCollision();
            return;
        }

        // 일반 지형과 충돌 - 스몰태우리 생성 후 제거
        HandleGroundCollision();
    }

    /// <summary>
    /// Shield 태그 확인
    /// </summary>
    /// <param name="other">확인할 콜라이더</param>
    /// <returns>Shield 여부</returns>
    private bool IsShield(Collider other)
    {
        return other.CompareTag(shieldTag);
    }

    /// <summary>
    /// 무시할 충돌인지 확인
    /// </summary>
    /// <param name="other">확인할 콜라이더</param>
    /// <returns>무시 여부</returns>
    private bool ShouldIgnoreCollision(Collider other)
    {
        int triggerLayer = other.gameObject.layer;
        return ((1 << triggerLayer) & ignoreCollisionLayers) != 0;
    }

    /// <summary>
    /// Shield와 충돌 시 처리
    /// </summary>
    private void HandleShieldCollision()
    {
        hasCollided = true;
        StopAutoDestroyTimer();

        // 스몰태우리 생성 없이 제거되므로 카운트 감소 필요
        ReturnToPoolWithoutSpawn();
    }

    /// <summary>
    /// 무시 레이어(Player 등)와 충돌 시 처리
    /// </summary>
    private void HandleIgnoreLayerCollision()
    {
        hasCollided = true;
        StopAutoDestroyTimer();

        // 스몰태우리 생성 없이 제거되므로 카운트 감소 필요
        ReturnToPoolWithoutSpawn();
    }

    /// <summary>
    /// 지형과 충돌 시 처리
    /// </summary>
    private void HandleGroundCollision()
    {
        hasCollided = true;
        StopAutoDestroyTimer();

        // CHM 추가함: 구체 형태로 닿은 지점에 생성
        Vector3 spawnPosition = GetGroundPosition();

        // 스몰태우리 생성
        SpawnSmallTaewoori(spawnPosition);

        // 파티클 제거
        ReturnToPool();
    }

    /// <summary>
    /// 구체 형태로 지면 감지하여 정확한 위치 계산
    /// </summary>
    /// <returns>스몰태우리 생성 위치</returns>
    private Vector3 GetGroundPosition()
    {
        RaycastHit hit;

        // 구체 형태로 아래쪽 감지
        if (Physics.SphereCast(transform.position, sphereRadius, Vector3.down, out hit))
        {
            // 닿은 지점에서 0.5만큼 위에 생성
            return hit.point + Vector3.up * 0.2f;
        }

        // 감지 실패시 기존 방식 사용
        return transform.position + Vector3.up * 0.2f;
    }

    /// <summary>
    /// 스몰태우리 생성
    /// </summary>
    /// <param name="position">생성 위치</param>
    private void SpawnSmallTaewoori(Vector3 position)
    {
        if (TaewooriPoolManager.Instance != null && originTaewoori != null)
        {
            TaewooriPoolManager.Instance.PoolSpawnSmallTaewoori(position, originTaewoori);
        }
    }
    #endregion

    #region 자동 파괴 시스템
    /// <summary>
    /// 자동 파괴 타이머 시작
    /// </summary>
    private void StartAutoDestroyTimer()
    {
        if (autoDestroyCoroutine != null)
        {
            StopCoroutine(autoDestroyCoroutine);
        }
        autoDestroyCoroutine = StartCoroutine(AutoDestroyCoroutine());
    }

    /// <summary>
    /// 자동 파괴 타이머 중지
    /// </summary>
    private void StopAutoDestroyTimer()
    {
        if (autoDestroyCoroutine != null)
        {
            StopCoroutine(autoDestroyCoroutine);
            autoDestroyCoroutine = null;
        }
    }

    /// <summary>
    /// 자동 파괴 코루틴
    /// </summary>
    /// <returns>코루틴</returns>
    private IEnumerator AutoDestroyCoroutine()
    {
        yield return new WaitForSeconds(autoDestroyTime);

        if (!hasCollided && gameObject.activeInHierarchy)
        {
            // 시간 초과로 제거 - 스몰태우리 생성 없음
            ReturnToPoolWithoutSpawn();
        }
    }
    #endregion

    #region 풀 반환
    /// <summary>
    /// 일반 풀 반환 (스몰태우리 생성 후)
    /// </summary>
    private void ReturnToPool()
    {
        if (TaewooriPoolManager.Instance != null)
        {
            TaewooriPoolManager.Instance.ReturnFireParticleToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 스몰태우리 생성 없이 풀 반환
    /// </summary>
    private void ReturnToPoolWithoutSpawn()
    {
        if (TaewooriPoolManager.Instance != null)
        {
            TaewooriPoolManager.Instance.ReturnFireParticleToPoolWithoutSpawn(gameObject, originTaewoori);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
}
