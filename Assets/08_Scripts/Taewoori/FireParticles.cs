using UnityEngine;

public class FireParticles : MonoBehaviour
{
    [SerializeField] private LayerMask ignoreCollisionLayers; // 레이어 마스크
    [SerializeField] private string shieldTag = "Shield"; // Shield 태그는 별도로 유지

    private Taewoori originTaewoori;

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }

        // 레이어 기반 충돌 무시 설정
        int myLayer = gameObject.layer;
        int playerLayer = LayerMask.NameToLayer("Player");
        int taewooriLayer = LayerMask.NameToLayer("Taewoori");
    }

    public void SetOriginTaewoori(Taewoori taewoori)
    {
        originTaewoori = taewoori;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Shield 태그 체크 - 이 태그가 있으면 충돌 무시
        if (collision.gameObject.CompareTag(shieldTag))
        {
            return;
        }

        // 레이어 마스크를 사용한 충돌 무시 (Player, Taewoori 등)
        int collisionLayer = collision.gameObject.layer;
        if (((1 << collisionLayer) & ignoreCollisionLayers) != 0)
        {
            return;
        }

        // 충돌 위치 가져오기
        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 collisionPoint = contact.point;

            // 작은 태우리 생성
            if (TaewooriPoolManager.Instance != null && originTaewoori != null)
            {
                TaewooriPoolManager.Instance.PoolSpawnSmallTaewoori(collisionPoint, originTaewoori);

            }

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
}
