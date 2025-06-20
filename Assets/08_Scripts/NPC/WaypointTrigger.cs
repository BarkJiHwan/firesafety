using UnityEngine;

public enum WaypointType
{
    Start,
    End
}

public class WaypointTrigger : MonoBehaviour
{
    private LayerMask playerLayerMask = 1 << 0;
    private FloorManager floorManager;
    private WaypointType waypointType;
    private bool hasTriggered = false;

    public void Initialize(FloorManager manager, WaypointType type)
    {
        floorManager = manager;
        waypointType = type;
    }

    public void SetPlayerLayerMask(LayerMask mask)
    {
        playerLayerMask = mask;
    }

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

    private bool IsPlayerLayer(int layer)
    {
        return (playerLayerMask & (1 << layer)) != 0;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
