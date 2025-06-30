using System;
using UnityEngine;

[Serializable]
public struct SyncVector
{
    public bool x;
    public bool y;
    public bool z;
    public bool w;
}

public class ObjectSyncronizer : MonoBehaviour
{
    public SyncVector position;
    public SyncVector rotation;
    public GameObject syncTarget;
    public ScaledHeightSyncronizer scaledHeightSyncronizer;

    private void FixedUpdate()
    {
        SyncPosition();
        SyncRotation();
    }

    private void SyncPosition()
    {
        Vector3 targetPos = syncTarget.transform.position;
        Vector3 myPos = transform.position;

        // bool 값에 따라 업데이트
        float x = position.x ? myPos.x : targetPos.x;
        float y = position.y ? myPos.y : targetPos.y;
        float z = position.z ? myPos.z : targetPos.z;

        // ScaledHeightSYncronizer 있을경우 y값 대체함
        if (scaledHeightSyncronizer != null)
        {
            y = scaledHeightSyncronizer.CalculateScaledHeight();
        }

        syncTarget.transform.position = new Vector3(x, y, z);
    }

    private void SyncRotation()
    {
        Quaternion targetRot = syncTarget.transform.rotation;
        Quaternion myRot = transform.rotation;

        // bool 값에 따라 업데이트
        float x = rotation.x ? myRot.x : targetRot.x;
        float y = rotation.y ? myRot.y : targetRot.y;
        float z = rotation.z ? myRot.z : targetRot.z;
        float w = rotation.w ? myRot.w : targetRot.w;

        syncTarget.transform.rotation = new Quaternion(x, y, z, w);
    }
}
