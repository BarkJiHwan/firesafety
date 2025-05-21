using UnityEngine;

public class SmallTaewoori : BaseTaewoori
{
    private Taewoori originTaewoori;

    // 공개 프로퍼티 추가 - 원본 태우리 접근용
    public Taewoori OriginTaewoori => originTaewoori;

    public void Initialize(TaewooriPoolManager taewooriManager, Taewoori taewoori)
    {
        manager = taewooriManager;
        originTaewoori = taewoori;

        // 체력 초기화 (부모 클래스 메서드 사용)
        InitializeHealth();

        ResetState();

    }

    public override void Die()
    {
        if (isDead)
            return;

        isDead = true;


        // 풀로 반환 (TaewooriPoolManager에서 발사체 카운트 자동 처리)
        if (manager != null)
        {
            manager.ReturnSmallTaewooriToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
