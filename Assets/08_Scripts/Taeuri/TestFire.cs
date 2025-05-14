using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestFire : MonoBehaviour
{
    [SerializeField] private GameObject spawnPrefab;  // 생성할 프리팹      
    [SerializeField] private string[] ignoreCollisionTags = { "Player", "Taeuri" };  // 충돌 무시할 태그들 (옵션)
    [SerializeField] private float effectDuration = 3f;  // 충돌 효과 지속 시간

    private void OnCollisionEnter(Collision collision)
    {
        // 특정 태그를 가진 오브젝트와의 충돌 무시 (필요한 경우)
        foreach (string tag in ignoreCollisionTags)
        {
            if (collision.gameObject.CompareTag(tag))
            {
                return;  // 무시할 태그와 충돌했으면 처리하지 않음
            }
        }

        // 충돌 위치 가져오기
        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 collisionPoint = contact.point;

            // 충돌 표면의 방향으로 회전 설정
            //Quaternion rotation = Quaternion.LookRotation(contact.normal);

            // 충돌 위치에 새 프리팹 생성
            if (spawnPrefab != null)
            {
                GameObject effect;

                if (TestPoolManager.Instance != null)
                {
                    // 풀에서 효과 오브젝트 가져오기
                    effect = TestPoolManager.Instance.Get(spawnPrefab, collisionPoint, Quaternion.identity);

                    // 일정 시간 후 풀로 반환
                    //TestPoolManager.Instance.ReleaseAfterDelay(effect, effectDuration);
                }
                else
                {
                    // 풀 매니저가 없을 경우 기존 방식으로 생성
                    effect = Instantiate(spawnPrefab, collisionPoint, Quaternion.identity);
                    Destroy(effect, effectDuration);
                }

                // 디버그 로그
                Debug.Log("충돌 효과 생성됨: " + collisionPoint);
            }

            // 발사체를 풀로 반환
            if (TestPoolManager.Instance != null)
            {
                TestPoolManager.Instance.Release(gameObject);
            }
            else
            {
                // 풀 매니저가 없을 경우 기존 방식으로 삭제
                Destroy(gameObject);
            }
        }
    }
}
