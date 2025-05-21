using System;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;


public class PlayerBehavior : MonoBehaviour
{
    /* 플레이어의 상태 나타내는 변수 */
    private bool isSitting;
    private bool isGrabbing;
    private bool isMoving;

    public XROrigin playerOrigin;
    public Camera playerCam;

    public bool IsSitting => isSitting;
    public bool IsGrabbing => isGrabbing;
    public bool IsMoving => isMoving;

    private void LateUpdate()
    {
        Vector3 playerCamRot = playerCam.transform.rotation.eulerAngles;
        Vector3 currentRot = gameObject.transform.rotation.eulerAngles;
        Vector3 updatedRot = new Vector3(currentRot.x, playerCamRot.y, currentRot.z);

        gameObject.transform.rotation = Quaternion.Euler(updatedRot);
        gameObject.transform.position = playerOrigin.transform.position;
    }
}
