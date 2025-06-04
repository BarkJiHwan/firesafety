using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasMeshRootCtrl : MonoBehaviour
{
    [SerializeField] GameObject curvedMesh;

    MakeCurvedMesh curvedManager;

    void Awake()
    {
        curvedManager = curvedMesh.GetComponent<MakeCurvedMesh>();
    }

    void Start()
    {
        
    }

    void LateUpdate()
    {
        FollowUser();
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
}
