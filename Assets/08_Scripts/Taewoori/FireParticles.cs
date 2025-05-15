using UnityEngine;

public class FireParticles : MonoBehaviour
{
    [SerializeField] private string[] ignoreCollisionTags = { "Player", "Taewoori" };

    private Taewoori originTaewoori;

    
    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 회전을 고정 (리지드바디가 회전하지 않도록 함)
            rb.freezeRotation = true;
        }
    }
    public void SetOriginTaewoori(Taewoori taewoori)
    {
        originTaewoori = taewoori;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 특정 태그 충돌 무시
        foreach (string tag in ignoreCollisionTags)
        {
            if (collision.gameObject.CompareTag(tag))
            {
                return;
            }
        }
        //if (collision.gameObject.CompareTag("Taewoori"))
        //{
        //    return;
        //}

            // 충돌 위치 가져오기
            if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 collisionPoint = contact.point;

            // 작은 태우리 생성
            if (TaewooriPoolManager.Instance != null && originTaewoori != null)
            {
                TaewooriPoolManager.Instance.PoolSpawnSmallTaewoori(collisionPoint, originTaewoori);
                Debug.Log($"[최현민] 발사체가 충돌하여 작은 태우리 생성: 위치={collisionPoint}");

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
