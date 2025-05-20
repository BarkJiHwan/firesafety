using System;
using UnityEngine;

public class PlayerBehavior : MonoBehaviour
{
    /* 플레이어의 상태 나타내는 변수 */
    private bool isSitting;
    private bool isGrabbing;
    private bool isMoving;

    public Camera playerCam;

    public bool IsSitting => isSitting;
    public bool IsGrabbing => isGrabbing;
    public bool IsMoving => isMoving;

    private void FixedUpdate()
    {
        throw new NotImplementedException();
    }
}
