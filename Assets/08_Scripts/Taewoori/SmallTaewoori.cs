public class SmallTaewoori : BaseTaewoori
{
    private Taewoori originTaewoori;

    // 공개 프로퍼티 추가 - 원본 태우리 접근용
    public Taewoori OriginTaewoori => originTaewoori;

    public void Initialize(TaewooriPoolManager taewooriManager, Taewoori taewoori)
    {
        manager = taewooriManager;
        originTaewoori = taewoori;

        // 스몰 태우리 생성 카운트 증가 (직접 관리)
        if (manager != null && originTaewoori != null)
        {
            manager.IncrementSmallTaewooriCount(originTaewoori);
        }

        // 체력 초기화 (부모 클래스 메서드 사용)
        InitializeHealth();

        ResetState();
    }

    // 카운트 증가 없이 초기화하는 함수 (파이어 파티클에서 이미 카운트 증가했을 때 사용)
    public void InitializeWithoutCountIncrement(TaewooriPoolManager taewooriManager, Taewoori taewoori)
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

        // 스몰 태우리 카운트 감소 (직접 관리)
        if (manager != null && originTaewoori != null)
        {
            manager.DecrementSmallTaewooriCount(originTaewoori);
        }

        // 풀로 반환
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
