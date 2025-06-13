using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasMeshRootCtrl : MonoBehaviour
{
    [SerializeField] GameObject canvas;
    [SerializeField] GameObject curvedMesh;
    [SerializeField] float angleThreshold = 30f;
    [SerializeField] bool isPlayerMove;

    Transform xrCam;
    MakeCurvedMesh curvedManager;

    RectTransform canvasTransform;
    float lastYAngle;

    void Awake()
    {
        curvedManager = curvedMesh.GetComponent<MakeCurvedMesh>();
    }

    void Start()
    {
        if (canvas != null)
        {
            canvasTransform = canvas.GetComponent<RectTransform>();
            canvasTransform.SetParent(transform, false);
        }
        xrCam = Camera.main.transform;
        lastYAngle = GetYaw(xrCam.forward);
        FollowUser();
    }

    void LateUpdate()
    {
        if(xrCam == null)
        {
            return;
        }
        float currentYaw = GetYaw(xrCam.forward);
        float angleDelta = Mathf.Abs(Mathf.DeltaAngle(lastYAngle, currentYaw));

        if (isPlayerMove == true)
        {
            FollowUserMove();

            if (angleDelta > angleThreshold)
            {
                FollowUserRotation(currentYaw);
                lastYAngle = currentYaw;
            }
        }
        else
        {
            if (angleDelta > angleThreshold)
            {
                FollowUser();
                lastYAngle = currentYaw;
            }
        }
    }

    void FollowUserMove()
    {
        //Vector3 forward = new Vector3(xrCam.forward.x, 0, xrCam.forward.z).normalized;

        //Quaternion rot = Quaternion.LookRotation(forward);
        //Vector3 rotatedOffset = rot * curvedManager.xrRigCurvedMeshDist;

        //Vector3 pos = xrCam.position + new Vector3(rotatedOffset.x, -0.9f, rotatedOffset.z);
        //transform.position = pos;

        Quaternion rot = Quaternion.Euler(0, lastYAngle, 0);
        Vector3 rotatedOffset = rot * curvedManager.xrRigCurvedMeshDist;

        Vector3 pos = xrCam.position + new Vector3(rotatedOffset.x, -0.9f, rotatedOffset.z);
        transform.position = pos;
    }

    void FollowUser()
    {
        Vector3 forward = new Vector3(xrCam.forward.x, 0, xrCam.forward.z).normalized;

        Quaternion rot = Quaternion.LookRotation(forward);
        Vector3 rotatedOffset = rot * curvedManager.xrRigCurvedMeshDist;

        Vector3 pos = xrCam.position + new Vector3(rotatedOffset.x, 0, rotatedOffset.z);

        pos.y = 0;

        transform.position = pos;
        transform.rotation = rot;
    }

    void FollowUserRotation(float angle)
    {
        //Vector3 forward = new Vector3(xrCam.forward.x, 0, xrCam.forward.z).normalized;
        //Quaternion rot = Quaternion.LookRotation(forward);

        //Vector3 rotatedOffset = rot * curvedManager.xrRigCurvedMeshDist;

        //Vector3 pos = new Vector3(xrCam.position.x + rotatedOffset.x, transform.position.y, xrCam.position.z + rotatedOffset.z);

        //transform.position = pos;
        //transform.rotation = rot;

        Quaternion rot = Quaternion.Euler(0, angle, 0);
        transform.rotation = rot;
    }

    float GetYaw(Vector3 forward)
    {
        return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }
}
