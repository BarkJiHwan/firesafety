using System.Collections;
using UnityEngine;

public class FireParticles : MonoBehaviour
{
    [SerializeField] private LayerMask ignoreCollisionLayers; // 레이어 마스크
    [SerializeField] private string shieldTag = "Shield"; // Shield 태그는 별도로 유지
    [SerializeField] private float autoDestroyTime = 5f; // 자동 파괴 시간 (5초로 증가)

    private Taewoori originTaewoori;
    private bool hasCollided = false;
    private Coroutine autoDestroyCoroutine;

    // 프로퍼티 추가
    public Taewoori OriginTaewoori => originTaewoori;

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

    private void OnDisable()
    {
        // 비활성화될 때 코루틴 정리
        if (autoDestroyCoroutine != null)
        {
            StopCoroutine(autoDestroyCoroutine);
            autoDestroyCoroutine = null;
        }
    }

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
            hasCollided = true;
            if (autoDestroyCoroutine != null)
            {
                StopCoroutine(autoDestroyCoroutine);
                autoDestroyCoroutine = null;
            }

            // 스몰 태우리 생성 없이 제거되므로 카운트 감소 필요
            if (TaewooriPoolManager.Instance != null)
            {
                TaewooriPoolManager.Instance.ReturnFireParticleToPoolWithoutSpawn(gameObject, originTaewoori);
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }

        // 레이어 마스크를 사용한 충돌 무시 (Player, Taewoori 등) - 그냥 통과 (제거 안함)
        int triggerLayer = other.gameObject.layer;
        if (((1 << triggerLayer) & ignoreCollisionLayers) != 0)
        {
            return; // 파이어 파티클은 계속 날아감
        }

        // 일반 트리거 접촉 - 즉시 스몰태우리 생성하고 파괴 (지면, 벽 등)
        hasCollided = true;

        // 자동 파괴 코루틴 중지
        if (autoDestroyCoroutine != null)
        {
            StopCoroutine(autoDestroyCoroutine);
            autoDestroyCoroutine = null;
        }

        Vector3 contactPoint = transform.position; // 트리거는 정확한 충돌점이 없으므로 현재 위치 사용

        // 즉시 스몰태우리 생성
        if (TaewooriPoolManager.Instance != null && originTaewoori != null)
        {
            GameObject spawnedSmallTaewoori = TaewooriPoolManager.Instance.PoolSpawnSmallTaewoori(contactPoint, originTaewoori);


        }

        // 즉시 파괴
        DestroyParticle();
    }

    // 자동 파괴 코루틴 (충돌하지 않고 시간이 지났을 때)
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

    // 파티클 파괴 처리
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
}
