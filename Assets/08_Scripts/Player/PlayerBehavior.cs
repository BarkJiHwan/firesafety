using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;

/*
 * 플레이어의 행동을 담당하는 클래스 입니다.
 * 카메라의 움직임을 받아 모델에 전달합니다
 */
public class PlayerBehavior : MonoBehaviour
{
    /* 플레이어의 상태 나타내는 변수 */
    private bool _isMoving;

    public XROrigin playerOrigin;
    public PhotonView photonView;

    private Camera _playerCam;
    private GameObject _playerCamOffset;

    public bool IsMoving => _isMoving;

    // 내꺼 아니면 스크립트 자동으로 꺼지게하기
    private void Awake()
    {
        _playerCam = playerOrigin.Camera;
        _playerCamOffset = playerOrigin.CameraFloorOffsetObject;

        if (photonView != null && !photonView.IsMine)
        {
            this.enabled = false;
        }
    }

    /* Position 업데이트 및 움직이는 상테인지 체크 */
    private void UpdatePosition()
    {
        Vector3 lastPos = gameObject.transform.position;
        Vector3 updatePos;

        if (playerOrigin.RequestedTrackingOriginMode == XROrigin.TrackingOriginMode.Floor)
        {
            updatePos = new Vector3(_playerCam.transform.position.x, 0, _playerCam.transform.position.z);
        }
        else
        {
            updatePos = playerOrigin.transform.position;
        }

        if (Vector3.Distance(lastPos, updatePos) >= 0.01f)
        {
            _isMoving = true;
        }
        else
        {
            _isMoving = false;
        }

        gameObject.transform.position = updatePos;
    }

    private void FixedUpdate()
    {
        Vector3 playerCamRot = _playerCam.transform.rotation.eulerAngles;
        Vector3 currentRot = gameObject.transform.rotation.eulerAngles;
        Vector3 updatedRot = new Vector3(currentRot.x, playerCamRot.y, currentRot.z);

        gameObject.transform.rotation = Quaternion.Euler(updatedRot);
        UpdatePosition();
    }
}
