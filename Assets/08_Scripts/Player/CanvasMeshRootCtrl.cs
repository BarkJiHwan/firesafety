using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasMeshRootCtrl : MonoBehaviour
{
    [SerializeField] GameObject canvas;
    [SerializeField] GameObject curvedMesh;
    [SerializeField] float angleThreshold = 30f;
    public bool isPlayerMove;

    Transform xrCam;
    MakeCurvedMesh curvedManager;

    RectTransform canvasTransform;
    float lastYAngle;

    Camera uiCam;

    bool isMade;

    void Awake()
    {
        curvedManager = curvedMesh.GetComponent<MakeCurvedMesh>();
        uiCam = transform.GetComponentInChildren<Camera>();
    }

    void Start()
    {
        if (canvas != null)
        {
            canvasTransform = canvas.GetComponent<RectTransform>();
        }
        if (isPlayerMove == false)
        {
            canvasTransform.SetParent(transform, false);
        }
        else
        {
            uiCam.transform.parent = null;
        }
        // 캐릭터가 생성된 후에 해야 할 수 있음
        if(Camera.main != null)
        {
            xrCam = Camera.main.transform;
            lastYAngle = GetYaw(xrCam.forward);
            FollowUser();
            isMade = true;
        }
    }

    void LateUpdate()
    {
        if(Camera.main == null)
        {
            return;
        }
        else if(isMade == false)
        {
            xrCam = Camera.main.transform;
            lastYAngle = GetYaw(xrCam.forward);
            FollowUser();
            isMade = true;
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
        Quaternion rot = Quaternion.Euler(0, angle, 0);
        transform.rotation = rot;
    }

    float GetYaw(Vector3 forward)
    {
        return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }
}
