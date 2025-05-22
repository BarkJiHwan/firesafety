using System;
using UnityEngine;

public class Taewoori : BaseTaewoori
{
    // 태우리 생성 및 파괴 이벤트 추가
    public static event Action<Taewoori, FireObjScript> OnTaewooriSpawned;
    public static event Action<Taewoori, FireObjScript> OnTaewooriDestroyed;

    [Header("포물선 발사 설정")]
    [SerializeField] private float throwForce = 3f; // 던지는 힘 (작은 값으로 짧은 거리)
    [SerializeField] private float upwardForce = 2f; // 위쪽 힘 (포물선 높이)
    [SerializeField] private float fireRadius = 2f; // 태우리 주위 발사 반지름
    [SerializeField] private bool useRandomDirection = true; // 랜덤 방향 또는 원형 패턴

    [Header("발사체 설정")]
    [SerializeField] private int maxSmallTaewooriCount = 4;
    [SerializeField] private float projectileCooldown = 2.0f;
    [SerializeField] private float spawnHeight = 0.5f;

    private bool _isDead = false;
    public bool IsDead => _isDead;

    private float _coolTime;
    private FireObjScript sourceFireObj;

    public int MaxSmallTaewooriCount => maxSmallTaewooriCount;
    public FireObjScript SourceFireObj => sourceFireObj;

    public void Initialize(TaewooriPoolManager taewooriManager, FireObjScript fireObj)
    {
        manager = taewooriManager;
        sourceFireObj = fireObj;
        sourceFireObj.SetActiveTaewoori(this);
        InitializeHealth();
        ResetState();
        OnTaewooriSpawned?.Invoke(this, sourceFireObj);
    }

    protected override void ResetState()
    {
        base.ResetState();
        _coolTime = 0f;
        _isDead = false;
    }

    public void Update()
    {
        if (sourceFireObj == null)
            return;

        _coolTime += Time.deltaTime;

        if (manager != null && projectileCooldown <= _coolTime &&
            manager.CanLaunchProjectile(this, maxSmallTaewooriCount))
        {
            LaunchProjectile();
        }
    }

    private void LaunchProjectile()
    {
        // 간단한 포물선 발사
        Vector3 launchPosition = transform.position + Vector3.up * spawnHeight;

        // 태우리 주위 원형 타겟 지점 계산
        Vector3 targetPosition = CalculateCircularTarget();
        Vector3 horizontalDirection = (targetPosition - transform.position).normalized;

        // 간단한 포물선 속도 계산
        Vector3 launchVelocity = horizontalDirection * throwForce + Vector3.up * upwardForce;

        if (manager != null)
        {
            Quaternion launchRotation = Quaternion.LookRotation(launchVelocity.normalized);
            GameObject projectile = manager.PoolSpawnFireParticle(launchPosition, launchRotation, this);

            if (projectile != null)
            {
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.AddForce(launchVelocity, ForceMode.Impulse);
                    _coolTime = 0f;
                }
            }
        }
    }

    private Vector3 CalculateCircularTarget()
    {
        if (useRandomDirection)
        {
            // 랜덤 방향으로 반지름만큼 떨어진 지점
            float randomAngle = UnityEngine.Random.Range(0f, 360f);
            Vector3 direction = new Vector3(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(randomAngle * Mathf.Deg2Rad)
            );
            return transform.position + direction * fireRadius;
        }
        else
        {
            // 순서대로 원형 패턴 (8방향)
            int directions = 8;
            float angleStep = 360f / directions;
            float currentAngle = (Time.time * 50f) % 360f; // 시간에 따라 회전

            Vector3 direction = new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            );
            return transform.position + direction * fireRadius;
        }
    }

    public override void Die()
    {
        if (_isDead)
            return;

        _isDead = true;
        isDead = true;

        OnTaewooriDestroyed?.Invoke(this, sourceFireObj);

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
