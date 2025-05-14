using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_02 : MonoBehaviour, IDamageable
{
    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 20f;        // 최대 체력
    [SerializeField] private float currentHealth;          // 현재 체력
    //[SerializeField] private GameObject deathEffectPrefab; // 사망 시 생성할 효과 (선택 사항)

    private bool isDead = false;                           // 사망 상태

    // 풀에서 가져왔을 때 또는 생성됐을 때 초기화
    private void OnEnable()
    {
        ResetObject();
    }

    // 오브젝트 상태 초기화
    private void ResetObject()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    // IDamageable 인터페이스 구현
    public void TakeDamage(float damage)
    {
        // 이미 사망 상태면 처리하지 않음
        if (isDead)
            return;

        // 현재 체력에서 데미지 차감
        currentHealth -= damage;

        // 디버그 로그
        Debug.Log($"{gameObject.name}이(가) {damage}의 데미지를 받음. 남은 체력: {currentHealth}");

        // 체력이 0 이하면 사망 처리
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 사망 처리 함수
    public void Die()
    {
        // 이미 사망 상태면 중복 처리 방지
        if (isDead)
            return;

        // 사망 상태 설정
        isDead = true;

        // 사망 효과 생성 (있는 경우)
        //if (deathEffectPrefab != null)
        //{
        //    if (TestPoolManager.Instance != null)
        //    {
        //        GameObject effect = TestPoolManager.Instance.Get(deathEffectPrefab, transform.position, Quaternion.identity);
        //
        //        // 사망 효과는 3초 후 풀로 반환
        //        Test_02 deathEffectComponent = effect.GetComponent<Test_02>();
        //        if (deathEffectComponent == null)
        //        {
        //            // 사망 효과에 Test_02 스크립트가 없으면 수동으로 지연 반환
        //            TestPoolManager.Instance.ReleaseAfterDelay(effect, 3f);
        //        }
        //    }
        //    else
        //    {
        //        GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        //        Destroy(effect, 3f);
        //    }
        //}

        // 오브젝트 풀로 반환
        if (TestPoolManager.Instance != null)
        {
            TestPoolManager.Instance.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
}
