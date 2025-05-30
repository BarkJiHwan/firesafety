using System.Collections;
using UnityEngine;

/// <summary>
/// 파이어파티클 클래스 - 태우리가 발사하는 발사체로 충돌 시 스몰태우리 생성
/// 물리 기반 포물선 궤도로 이동하며 지형이나 Shield와 충돌 처리
/// </summary>
public class FireParticles : MonoBehaviour
{
    #region 인스펙터 설정
    [SerializeField] private LayerMask ignoreCollisionLayers; // 레이어 마스크
    [SerializeField] private string shieldTag = "Shield"; // Shield 태그는 별도로 유지
    [SerializeField] private float autoDestroyTime = 5f; // 자동 파괴 시간 (5초로 증가)
    #endregion

    #region 변수 선언
    private Taewoori originTaewoori;
    private bool hasCollided = false;
    private Coroutine autoDestroyCoroutine;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 이 발사체를 생성한 원본 태우리 참조
    /// </summary>
    public Taewoori OriginTaewoori => originTaewoori;
    #endregion

    #region 유니티 라이프사이클
    /// <summary>
    /// 발사체 초기 설정 - 물리 속성 및 충돌 무시 설정
    /// </summary>
    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }

        // 태우리와 물리적 충돌 무시 설정
        if (originTaewoori != null)
        {
            Collider myCollider = GetComponent<Collider>();
            Collider taewooriCollider = originTaewoori.GetComponent<Collider>();

            if (myCollider != null && taewooriCollider != null)
            {
                Physics.IgnoreCollision(myCollider, taewooriCollider);
            }
        }

        // 레이어 기반 충돌 무시 설정
        int myLayer = gameObject.layer;
        int playerLayer = LayerMask.NameToLayer("Player");
        int taewooriLayer = LayerMask.NameToLayer("Taewoori");
    }

    /// <summary>
    /// 오브젝트 활성화 시 초기화 - 충돌 상태 리셋 및 자동 파괴 타이머 시작
    /// </summary>
    private void OnEnable()
    {
        hasCollided = false;

        // 활성화될 때마다 자동 파괴 타이머 시작
        if (autoDestroyCoroutine != null)
        {
            StopCoroutine(autoDestroyCoroutine);
        }
        autoDestroyCoroutine = StartCoroutine(AutoDestroyAfterTime());
    }

    /// <summary>
    /// 오브젝트 비활성화 시 코루틴 정리
    /// </summary>
    private void OnDisable()
    {
        // 비활성화될 때 코루틴 정리
        if (autoDestroyCoroutine != null)
        {
            StopCoroutine(autoDestroyCoroutine);
            autoDestroyCoroutine = null;
        }
    }
    #endregion

    #region 초기화 함수
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
            Collider myCollider = GetComponent<Collider>();
            Collider taewooriCollider = originTaewoori.GetComponent<Collider>();

            if (myCollider != null && taewooriCollider != null)
            {
                Physics.IgnoreCollision(myCollider, taewooriCollider);
            }
        }
    }
    #endregion

    #region 충돌 처리 시스템
    /// <summary>
    /// 트리거 충돌 처리 - Shield, 무시 레이어, 일반 지형에 따른 분기 처리
    /// </summary>
    /// <param name="other">충돌한 콜라이더</param>
    private void OnTriggerEnter(Collider other)
    {
        // 이미 충돌 처리되었으면 무시
        if (hasCollided)
        {
            return;
        }

        // Shield 태그 체크 - 이 태그가 있으면 바로 제거
        if (other.CompareTag(shieldTag))
        {
            Debug.Log("Shield와 접촉 - 즉시 파괴");
            HandleShieldCollision();
            return;
        }

        // 레이어 마스크를 사용한 충돌 무시 (Player, Taewoori 등) - 그냥 통과 (제거 안함)
        int triggerLayer = other.gameObject.layer;
        if (((1 << triggerLayer) & ignoreCollisionLayers) != 0)
        {
            return; // 파이어 파티클은 계속 날아감
        }

        // 일반 트리거 접촉 - 즉시 스몰태우리 생성하고 파괴 (지면, 벽 등)
        HandleGroundCollision();
    }

    /// <summary>
    /// Shield와 충돌 시 처리 - 스몰태우리 생성 없이 즉시 제거
    /// </summary>
    private void HandleShieldCollision()
    {
        hasCollided = true;
        StopAutoDestroyTimer();

        // 스몰 태우리 생성 없이 제거되므로 카운트 감소 필요
        if (TaewooriPoolManager.Instance != null)
        {
            TaewooriPoolManager.Instance.ReturnFireParticleToPoolWithoutSpawn(gameObject, originTaewoori);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 지형과 충돌 시 처리 - 스몰태우리 생성 후 제거
    /// </summary>
    private void HandleGroundCollision()
    {
        hasCollided = true;
        StopAutoDestroyTimer();

        Vector3 contactPoint = transform.position; // 트리거는 정확한 충돌점이 없으므로 현재 위치 사용

        // 즉시 스몰태우리 생성
        if (TaewooriPoolManager.Instance != null && originTaewoori != null)
        {
            GameObject spawnedSmallTaewoori = TaewooriPoolManager.Instance.PoolSpawnSmallTaewoori(contactPoint, originTaewoori);
        }

        // 즉시 파괴
        DestroyParticle();
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
    #endregion

    #region 자동 파괴 시스템
    /// <summary>
    /// 자동 파괴 코루틴 - 충돌하지 않고 시간이 지났을 때 스몰태우리 생성 없이 제거
    /// </summary>
    /// <returns>코루틴</returns>
    private IEnumerator AutoDestroyAfterTime()
    {
        yield return new WaitForSeconds(autoDestroyTime);

        if (!hasCollided && gameObject.activeInHierarchy)
        {
            // 스몰 태우리 생성 없이 제거되므로 카운트 감소 필요
            if (TaewooriPoolManager.Instance != null)
            {
                TaewooriPoolManager.Instance.ReturnFireParticleToPoolWithoutSpawn(gameObject, originTaewoori);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 파티클 일반 파괴 처리 - 스몰태우리 생성 후 풀로 반환
    /// </summary>
    private void DestroyParticle()
    {
        // 풀로 반환
        if (TaewooriPoolManager.Instance != null)
        {
            TaewooriPoolManager.Instance.ReturnFireParticleToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
}
