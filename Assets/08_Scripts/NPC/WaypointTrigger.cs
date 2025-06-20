using UnityEngine;

/// <summary>
/// 웨이포인트 타입
/// </summary>
public enum WaypointType
{
    Start,
    End
}

/// <summary>
/// 웨이포인트 트리거 컴포넌트
/// </summary>
public class WaypointTrigger : MonoBehaviour
{
    private FloorManager floorManager;
    private WaypointType waypointType;
    private bool hasTriggered = false;

    public void Initialize(FloorManager manager, WaypointType type)
    {
        floorManager = manager;
        waypointType = type;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || hasTriggered)
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

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
