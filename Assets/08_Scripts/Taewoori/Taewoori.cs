using UnityEngine;

public class Taewoori : MonoBehaviour, IDamageable
{
    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("발사체 설정")]
    [SerializeField] private float launchForce = 10f;
    [SerializeField] private float launchAngle = 45f;
    [SerializeField] private float randomRadius = 5f;
    [SerializeField] private int maxProjectiles = 4;
    [SerializeField] private float projectileCooldown = 2.0f; // 발사체 발사 쿨타임

    private float _coolTime;
    private Vector3 randomDirection;
    private bool isDead = false;

    private TaewooriPoolManager manager;
    private FireObjScript sourceFireObj; // 이 태우리를 생성한 화재 오브젝트

    public int MaxProjectiles => maxProjectiles;
    public FireObjScript SourceFireObj => sourceFireObj;

    public void Initialize(TaewooriPoolManager taewooriManager, FireObjScript fireObj)
    {
        manager = taewooriManager;
        sourceFireObj = fireObj;
        ResetState();
    }

    private void OnEnable()
    {
        ResetState();
    }

    private void ResetState()
    {
        currentHealth = maxHealth;
        _coolTime = 0f;
        isDead = false;
        GenerateRandomDirection();
    }

    private void GenerateRandomDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        randomDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
    }

    public void Update()
    {

        if (sourceFireObj == null)
        {
            return;
        }

        _coolTime += Time.deltaTime;

        // 발사 가능한 상태인지 확인
        if (manager != null && projectileCooldown <= _coolTime && manager.CanLaunchProjectile(this, maxProjectiles))
        {
            LaunchProjectile();
        }
    }

    private void LaunchProjectile()
    {
        // 새 랜덤 방향 생성
        GenerateRandomDirection();

        // 스폰 위치 계산
        Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);

        if (manager != null)
        {
            GameObject projectile = manager.PoolSpawnFireParticle(spawnPosition, Quaternion.identity, this);

            if (projectile != null)
            {
                Rigidbody rb = projectile.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // 각도를 라디안으로 변환
                    float radianAngle = launchAngle * Mathf.Deg2Rad;

                    // 수평 방향 계산
                    Vector3 horizontalDir = randomDirection.normalized;

                    // 포물선 발사 방향 계산
                    Vector3 direction = new Vector3(
                        horizontalDir.x,
                        Mathf.Sin(radianAngle),
                        horizontalDir.z).normalized;

                    // 힘 적용
                    rb.velocity = Vector3.zero;
                    rb.AddForce(direction * launchForce, ForceMode.Impulse);

                    // 쿨타임 리셋
                    _coolTime = 0f;

                    Debug.Log($"[최현민] {gameObject.name}이(가) 발사체 발사");
                }
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name}이(가) {damage}의 데미지를 받음. 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (manager != null)
        {
            manager.ReturnTaewooriToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
