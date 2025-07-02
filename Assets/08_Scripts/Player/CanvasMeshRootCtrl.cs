using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasMeshRootCtrl : MonoBehaviour
{
    [SerializeField] GameObject canvas;
    [SerializeField] GameObject curvedMesh;
    [SerializeField] float angleThreshold = 30f;
    [SerializeField] float rotationSpeed = 90f;
    public bool isPlayerMove;

    Transform xrCam;
    MakeCurvedMesh curvedManager;

    RectTransform canvasTransform;
    float lastYAngle;

    Camera uiCam;

    bool isMade;
    bool isRotating;

    float rotationStartAngle;
    float rotationTargetAngle;

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
            FollowUser(lastYAngle);
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
            FollowUser(lastYAngle);
            isMade = true;
        }
        float currentYaw = GetYaw(xrCam.forward);
        float angleDelta = Mathf.Abs(Mathf.DeltaAngle(lastYAngle, currentYaw));

        if (isPlayerMove == true)
        {
            if (isRotating == false)
            {
                FollowUserMove();
            }

            if (angleDelta > angleThreshold && isRotating == false)
            {
                rotationStartAngle = transform.eulerAngles.y;
                rotationTargetAngle = currentYaw;
                isRotating = true;
            }
            if (isRotating == true)
            {
                FollowUserRotation(rotationTargetAngle);
            }
        }
        else
        {
            if (angleDelta > angleThreshold && isRotating == false)
            {
                rotationStartAngle = transform.eulerAngles.y;
                rotationTargetAngle = currentYaw;
                isRotating = true;
            }
            if (isRotating == true)
            {
                FollowUser(rotationTargetAngle);
            }
        }
    }

    void FollowUserMove()
    {
        //float currentYaw = GetYaw(xrCam.forward);
        Quaternion rot = Quaternion.Euler(0, lastYAngle, 0);
        Vector3 rotatedOffset = rot * curvedManager.xrRigCurvedMeshDist;

        Vector3 pos = xrCam.position + new Vector3(rotatedOffset.x, -0.9f, rotatedOffset.z);
        transform.position = pos;
        transform.rotation = rot;
    }

    void FollowUser(float angle)
    {
        Quaternion targetRot = Quaternion.Euler(0, angle, 0);

        Vector3 rotatedOffset = targetRot * curvedManager.xrRigCurvedMeshDist;
        Vector3 targetPos = xrCam.position + new Vector3(rotatedOffset.x, 0f, rotatedOffset.z);
        targetPos.y = -0.2f;

        transform.position = targetPos;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

        float angleToTarget = Quaternion.Angle(transform.rotation, targetRot);
        if(angleToTarget < 1f)
        {
            transform.rotation = targetRot;
            isRotating = false;
            lastYAngle = angle;
        }
    }

    void FollowUserRotation(float angle)
    {
        Quaternion targetRot = Quaternion.Euler(0, angle, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);

        Quaternion currentRot = transform.rotation;
        Vector3 rotatedOffset = currentRot * curvedManager.xrRigCurvedMeshDist;
        Vector3 targetPos = xrCam.position + new Vector3(rotatedOffset.x, -0.9f, rotatedOffset.z);
        transform.position = targetPos;

        float angleToTarget = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, angle));
        if (angleToTarget < 1f)
        {
            transform.rotation = targetRot;
            isRotating = false;
            lastYAngle = angle;
        }
    }

    float GetYaw(Vector3 forward)
    {
        return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }
}
