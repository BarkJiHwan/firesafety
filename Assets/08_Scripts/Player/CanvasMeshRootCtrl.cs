using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasMeshRootCtrl : MonoBehaviour
{
    [SerializeField] GameObject curvedMesh;
    [SerializeField] float angleThreshold = 30f;

    Transform xrCam;
    MakeCurvedMesh curvedManager;

    float lastYAngle;

    void Awake()
    {
        curvedManager = curvedMesh.GetComponent<MakeCurvedMesh>();
    }

    void Start()
    {
        xrCam = Camera.main.transform;
        lastYAngle = GetYaw(xrCam.forward);
        FollowUser();
    }

    void LateUpdate()
    {
        float currentYaw = GetYaw(xrCam.forward);
        float angleDelta = Mathf.Abs(Mathf.DeltaAngle(lastYAngle, currentYaw));

        if(angleDelta > angleThreshold)
        {
            FollowUser();
            lastYAngle = currentYaw;
        }
    }

    void FollowUser()
    {
        Transform xrCam = Camera.main.transform;

        Vector3 forward = new Vector3(xrCam.forward.x, 0, xrCam.forward.z).normalized;

        Quaternion rot = Quaternion.LookRotation(forward);
        Vector3 rotatedOffset = rot * curvedManager.xrRigCurvedMeshDist;

        Vector3 pos = xrCam.position + new Vector3(rotatedOffset.x, 0, rotatedOffset.z);
        pos.y = 0;
        transform.position = pos;
        transform.rotation = rot;
    }

    float GetYaw(Vector3 forward)
    {
        return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }
}
