using UnityEngine;

/// <summary>
/// 튜토리얼용 태우리 - BaseTaewoori를 상속받아 체력 관리 기능 사용
/// 사망 시 플레이어의 소화기를 비활성화하는 특수 기능 포함
/// </summary>
public class TaewooriTutorial : BaseTaewoori
{
    #region 사망 처리 (BaseTaewoori 추상 메서드 구현)
    /// <summary>
    /// 튜토리얼 태우리 사망 처리 - 소화기 비활성화
    /// </summary>
    public override void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 사망 시 모든 플레이어의 소화기 비활성화
        DisableAllPlayerSuppressors();

        // 오브젝트 제거
        Destroy(gameObject);
    }

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
