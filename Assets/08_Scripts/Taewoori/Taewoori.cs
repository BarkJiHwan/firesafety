using System;
using UnityEngine;

public class Taewoori : BaseTaewoori
{
    // 태우리 생성 및 파괴 이벤트 추가
    public static event Action<Taewoori, FireObjScript> OnTaewooriSpawned;
    public static event Action<Taewoori, FireObjScript> OnTaewooriDestroyed;

    [Header("발사체 설정")]
    [SerializeField] private float launchForce = 10f;
    [SerializeField] private float launchAngle = 45f;
    [SerializeField] private float randomRadius = 5f;
    [SerializeField] private int maxProjectiles = 4;
    [SerializeField] private float projectileCooldown = 2.0f; // 발사체 발사 쿨타임
    [SerializeField] private float horizontalSpreadAngle = 180f; // 수평 방향 랜덤 각도 범위 (기본 180도)
    [SerializeField] private float verticalSpreadAngle = 30f; // 수직 방향 랜덤 각도 범위 (기본 30도)

    private bool _isDead = false;
    public bool IsDead => _isDead;

    private float _coolTime;
    private Vector3 randomDirection;
    private FireObjScript sourceFireObj; // 이 태우리를 생성한 화재 오브젝트

    public int MaxProjectiles => maxProjectiles;
    public FireObjScript SourceFireObj => sourceFireObj;

    public void Initialize(TaewooriPoolManager taewooriManager, FireObjScript fireObj)
    {
        manager = taewooriManager;
        sourceFireObj = fireObj;

        // 소스 화재 오브젝트에 자신을 활성 태우리로 등록
        sourceFireObj.SetActiveTaewoori(this);

        // 체력 초기화 (부모 클래스 메서드 사용)
        InitializeHealth();

        ResetState();

        // 태우리 생성 이벤트 발생
        OnTaewooriSpawned?.Invoke(this, sourceFireObj);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"<color=green>태우리 생성됨: {gameObject.name}, 소스: {sourceFireObj?.name}</color>");
#endif
    }

    protected override void ResetState()
    {
        base.ResetState();
        _coolTime = 0f;
        _isDead = false;
        GenerateRandomDirection();
    }

    private void GenerateRandomDirection()
    {
        // 1. 수평 방향 랜덤 각도 생성 (0-360도)
        float randomHorizontalAngle = UnityEngine.Random.Range(0f, 360f);

        // 2. 수직 방향 랜덤 각도 생성 (launchAngle 기준으로 ±수직 확산 각도)
        float randomVerticalAngle = launchAngle + UnityEngine.Random.Range(-verticalSpreadAngle / 2, verticalSpreadAngle / 2);

        // 3. 두 각도를 이용하여 3D 방향 벡터 생성
        // 수평 회전 적용
        Quaternion horizontalRotation = Quaternion.Euler(0, randomHorizontalAngle, 0);

        // 기본 앞 방향 벡터에 회전 적용
        Vector3 horizontalDir = horizontalRotation * Vector3.forward;

        // 수직 각도를 라디안으로 변환
        float verticalRadians = randomVerticalAngle * Mathf.Deg2Rad;

        // 최종 방향 벡터 계산 (수평 방향 + 수직 각도)
        randomDirection = new Vector3(
            horizontalDir.x,
            Mathf.Sin(verticalRadians),
            horizontalDir.z
        ).normalized;

    }

    public void Update()
    {
        // 소스 파이어 오브젝트가 없으면 업데이트 중지
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
            // 방향을 기반으로 회전 계산 - 파티클 시스템을 방향에 맞게 회전시킴
            Quaternion lookRotation = Quaternion.LookRotation(randomDirection);

            // 파티클 시스템이 -Z 방향으로 발사되도록 90도 추가 회전 (파티클 프리팹 설정에 따라 조정 필요)
            Quaternion fixedRotation = lookRotation * Quaternion.Euler(-90f, 0, 0);

            // 회전값을 적용하여 파티클 생성
            GameObject projectile = manager.PoolSpawnFireParticle(spawnPosition, fixedRotation, this);

            if (projectile != null)
            {
                Rigidbody rb = projectile.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // 힘 적용 - 이미 계산된 방향 벡터 사용
                    rb.velocity = Vector3.zero;
                    rb.AddForce(randomDirection * launchForce, ForceMode.Impulse);

                    // 쿨타임 리셋
                    _coolTime = 0f;

                }
            }
        }
    }

    public override void Die()
    {
        if (_isDead)
            return;

        _isDead = true;
        isDead = true; // 부모 클래스 변수도 설정

        // 태우리 파괴 이벤트 발생
        OnTaewooriDestroyed?.Invoke(this, sourceFireObj);


        // 풀로 반환
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
