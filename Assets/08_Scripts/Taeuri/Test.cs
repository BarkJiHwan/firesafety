using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour , IDamageable
{
    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 100f;        // 최대 체력
    [SerializeField] private float currentHealth;          // 현재 체력

    [SerializeField] private GameObject projectilePrefab;  // 발사할 프리팹
    [SerializeField] private float launchForce = 10f;      // 발사 힘
    [SerializeField] private float launchAngle = 45f;      // 발사 각도 (도)
    [SerializeField] private float randomRadius = 5f;      // 랜덤 발사 반경
    [SerializeField] private int maxProjectiles = 4;       // 최대 발사 개수
    
    
    [SerializeField] private float _coolTime;              // 현재 쿨타임
    [SerializeField] private float _taeuriTime;              // 태우리 쿨타임
    private int projectilesLaunched = 0;                   // 현재까지 발사한 개수
    private Vector3 randomDirection;                       // 랜덤 발사 방향
    private bool isDead = false;                           // 사망 상태
    private FireObjScript _parentFireObj;

    private void Start()
    {
        _coolTime = 0f;
        projectilesLaunched = 0;

        // 부모 FireObjScript 찾기
        _parentFireObj = GetComponentInParent<FireObjScript>();        

        // 초기 랜덤 방향 설정
        GenerateRandomDirection();
    }

    // 랜덤 방향 생성 함수
    private void GenerateRandomDirection()
    {
        // 360도 중 랜덤한 각도로 발사 방향 생성
        float randomAngle = Random.Range(0f, 360f);
        randomDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.up;
    }

    // 발사 함수
    public void LaunchProjectile()
    {
        // 최대 개수 체크
        if (projectilesLaunched >= maxProjectiles)
        {
            return; // 최대 개수 도달 시 더 이상 발사하지 않음
        }

        // 매번 발사할 때마다 새로운 랜덤 방향 생성
        GenerateRandomDirection();
        // 프리팹 생성
        // 가장 간결한 방법
        //GameObject projectile = Instantiate(projectilePrefab,
        //                                    new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z),
        //                                    Quaternion.identity);
        Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        GameObject projectile;

        if (TestPoolManager.Instance != null)
        {
            projectile = TestPoolManager.Instance.Get(projectilePrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            // 풀링을 사용할 수 없는 경우 직접 인스턴스화
            projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        }

        // 리지드바디 컴포넌트 가져오기
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // 각도를 라디안으로 변환
            float radianAngle = launchAngle * Mathf.Deg2Rad;

            // 수평 방향과 수직 방향을 분리하여 계산
            // randomDirection은 수평(XZ) 평면에서의 방향
            Vector3 horizontalDir = randomDirection.normalized;

            // 포물선 발사 방향 계산 (수평 + 수직 성분)
            Vector3 direction = new Vector3
            (   horizontalDir.x,
                Mathf.Sin(radianAngle),
                horizontalDir.z).normalized;

            // 힘 적용
            rb.AddForce(direction * launchForce, ForceMode.Impulse);

            // 발사 개수 증가 및 쿨타임 리셋
            projectilesLaunched++;
            _coolTime = 0f;

            // 디버그 로그 (테스트용)
            Debug.Log($"발사체 #{projectilesLaunched}/{maxProjectiles} 발사됨");
        }
        else
        {
            Debug.LogError("발사체에 Rigidbody 컴포넌트가 없습니다!");
        }
    }

    public void Update()
    {
        _coolTime += Time.deltaTime;

        // 소각 상태이고 최대 개수에 도달하지 않았을 때만 발사
        if (_parentFireObj.IsBurning == true && projectilesLaunched < maxProjectiles)
        {
            if (_taeuriTime <= _coolTime)
            {
                LaunchProjectile();
            }
        }
    }

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
