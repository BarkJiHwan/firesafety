using UnityEngine;

/// <summary>
/// 기본 태우리 클래스
/// </summary>
public class Taeuri : MonoBehaviour, IDamageable
{
    // 기본 변수
    [SerializeField] private float _health = 100f;
    [SerializeField] private float _feverTimeHealth = 30f;
    private int _score; // 아직 미정

    // 애니메이션 관련
    [SerializeField] private Animator _animator;

    // 풀링 매니저 참조
    private TaeuriPoolManager _poolManager;

    /// <summary>
    /// 풀링 매니저 참조 설정
    /// </summary>
    public void SetPoolManager(TaeuriPoolManager poolManager)
    {
        _poolManager = poolManager;
    }

    /// <summary>
    /// 피버타임 시 체력을 증가시키는 함수
    /// </summary>
    public void IncreaseFeverTimeHealth()
    {
        _health += _feverTimeHealth;
    }

    /// <summary>
    /// 데미지를 받아 체력을 감소시키는 함수 (인터페이스 구현)
    /// </summary>
    /// <param name="damage">받을 데미지 양</param>
    public void TakeDamage(float damage)
    {
        _health -= damage;

        if (_health <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 태우리가 죽을 때 호출되는 함수
    /// </summary>
    private void Die()
    {
        if (_poolManager != null)
        {
            // 죽는 이펙트 생성
            _poolManager.CreateDeathEffect(transform.position);

            // 풀로 반환
            _poolManager.ReleaseTaeuri(gameObject);
        }
        else
        {
            // 풀링 매니저가 없는 경우 그냥 비활성화
            gameObject.SetActive(false);
        }
    }
}
