using UnityEngine;

/// <summary>
/// 튜토리얼용 태우리 - BaseTaewoori를 상속받아 체력 관리 기능 사용
/// 사망 시 플레이어의 소화기를 비활성화하는 특수 기능 포함
/// </summary>
public class TaewooriTutorial : BaseTaewoori
{
    #region 사망 처리 (BaseTaewoori 메서드 오버라이드)
    /// <summary>
    /// 튜토리얼 태우리 사망 처리 - 애니메이션 재생 후 소화기 비활성화
    /// </summary>
    public override void Die()
    {
        if (isDead)
            return;

        // BaseTaewoori의 기본 Die() 호출 (애니메이션 포함)
        base.Die();

        // 추가로 소화기 비활성화
        DisableAllPlayerSuppressors();
    }

    /// <summary>
    /// Death 애니메이션 완료 후 최종 처리 - 오브젝트 파괴하지 않음
    /// </summary>
    protected override void PerformFinalDeath()
    {
        // 튜토리얼 매니저 오류 방지를 위해 파괴하지 않고 비활성화만
        gameObject.SetActive(false);
    }
    #endregion

    #region 소화기 비활성화
    /// <summary>
    /// 모든 플레이어의 소화기 비활성화
    /// </summary>
    private void DisableAllPlayerSuppressors()
    {
        var players = FindObjectsOfType<FireSuppressantManager>();

        foreach (var player in players)
        {
            if (player.pView != null && player.pView.IsMine)
            {
                var tutoSuppressor = player.tutoSuppressor;

                if (tutoSuppressor != null)
                {
                    tutoSuppressor.SetAmountZero();
                }
                else
                {
                    Debug.LogWarning("TutorialSuppressor를 찾을 수 없습니다.");
                }
            }
        }
    }
    #endregion
}
