using UnityEngine;

public class TestAttack : MonoBehaviour
{
    [Header("공격 설정")]
    [SerializeField] private float attackDamage = 10f; // 기본 데미지
    [SerializeField] private float attackRange = 5f; // 공격 범위
    [SerializeField] private string taewooriTag = "Taewoori"; // 태우리 태그    




    void Update()
    {
        // A키를 누르면 데미지 적용
        if (Input.GetKeyDown(KeyCode.A))
        {
            AttackNearbyTaewooris();
        }
    }

    private void AttackNearbyTaewooris()
    {
        // 주변의 모든 콜라이더 찾기
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        bool foundTarget = false;

        foreach (var hitCollider in hitColliders)
        {
            // 태그로 태우리 확인
            if (hitCollider.CompareTag(taewooriTag))
            {
                // IDamageable 인터페이스를 가진 컴포넌트 확인
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();

                if (damageable != null)
                {
                    // 데미지 적용
                    damageable.TakeDamage(attackDamage);
                    Debug.Log($"공격  {hitCollider.name}에게 {attackDamage} 데미지를 입혔습니다.");
                    foundTarget = true;
                }
            }
        }

        if (!foundTarget)
        {
            Debug.Log("주변에 공격 가능한 태우리가 없습니다.");
        }
    }

    // 디버그 시각화 (공격 범위 표시)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
