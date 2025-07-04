using UnityEngine;

public enum WaypointType
{
    Start,
    End
}

/// <summary>
/// 웨이포인트 트리거 - 플레이어 진입/탈출 감지
/// </summary>
public class WaypointTrigger : MonoBehaviour
{
    // 9번 레이어가 플레이어
    private LayerMask playerLayerMask = 1 << 9;
    private FloorManager floorManager;
    private WaypointType waypointType;
    private bool hasTriggered = false;

    /// <summary>
    /// 웨이포인트 초기화
    /// </summary>
    public void Initialize(FloorManager manager, WaypointType type)
    {
        floorManager = manager;
        waypointType = type;
    }

    /// <summary>
    /// 플레이어 레이어 마스크 설정
    /// </summary>
    public void SetPlayerLayerMask(LayerMask mask)
    {
        playerLayerMask = mask;
    }

    /// <summary>
    /// 트리거 진입 시 플레이어 감지 및 이벤트 처리
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerLayer(other.gameObject.layer) || hasTriggered)
            return;

        hasTriggered = true;

        switch (waypointType)
        {
            case WaypointType.Start:
                floorManager.OnStartWaypointTriggered();
                break;
            case WaypointType.End:
                floorManager.OnEndWaypointTriggered();
                break;
        }
    }

    /// <summary>
    /// 레이어가 플레이어인지 확인
    /// </summary>
    private bool IsPlayerLayer(int layer)
    {
        return (playerLayerMask & (1 << layer)) != 0;
    }

    /// <summary>
    /// 트리거 상태 리셋
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
