using System.Collections;
using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;

public class PlayerBehavior : MonoBehaviour
{
    private static readonly int _moving = Animator.StringToHash("IsMoving");

    /* 플레이어의 상태 나타내는 변수 */
    private bool _isSitting;
    private bool _isMoving;

    public XROrigin playerOrigin;
    public PhotonView photonView;

    private Camera _playerCam;
    private GameObject _playerCamOffset;
    private Animator _animator;

    //CHM - 소백이 관련
    [SerializeField] private GameObject sobaekPrefab;
    private GameObject sobaekInstance;

    public bool IsSitting => _isSitting;
    public bool IsMoving => _isMoving;

    // 내꺼 아니면 스크립트 자동으로 꺼지게하기
    private void Awake()
    {
        _playerCam = playerOrigin.Camera;
        _playerCamOffset = playerOrigin.CameraFloorOffsetObject;
        _animator = GetComponent<Animator>();

        if (!photonView.IsMine)
        {
            this.enabled = false;
            return;
        }
    }

    //CHM - 내 플레이어일 때만 소백이 생성
    private void Start()
    {
        if (photonView.IsMine)
        {
            StartCoroutine(CreateSobaekWhenReady());
        }
    }

    //CHM - 플레이어 준비되면 소백이 생성
    private IEnumerator CreateSobaekWhenReady()
    {
        // PlayerCam과 XR이 제대로 설정될 때까지 대기
        while (_playerCam == null || playerOrigin == null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f); // 지연시켜 줘야함

        CreateSobaek();
    }

    //CHM - 소백이 생성 메서드
    private void CreateSobaek()
    {
        if (sobaekPrefab == null)
        {
            return;
        }

        sobaekInstance = Instantiate(sobaekPrefab);
        Sobaek sobaekScript = sobaekInstance.GetComponent<Sobaek>();

        if (sobaekScript != null)
        {
            // 플레이어 카메라 설정
            sobaekScript.Player = _playerCam.transform;
        }
        else
        {
            Destroy(sobaekInstance);
        }
    }


    //CHM - 플레이어 파괴시 소백이도 같이 파괴
    private void OnDestroy()
    {
        if (sobaekInstance != null)
        {
            Destroy(sobaekInstance);
        }
    }

    /* Position 업데이트 및 움직이는 상테인지 체크 */
    private void UpdatePosition()
    {
        Vector3 lastPos = gameObject.transform.position;
        Vector3 updatePos;

        if (playerOrigin.RequestedTrackingOriginMode == XROrigin.TrackingOriginMode.Floor)
        {
            updatePos = _playerCam.transform.position - _playerCamOffset.transform.position;
        }
        else
        {
            updatePos = playerOrigin.transform.position;
        }

        if (lastPos - updatePos != Vector3.zero)
        {
            _isMoving = true;
        }
        else
        {
            _isMoving = false;
        }

        gameObject.transform.position = updatePos;
        _animator.SetBool(_moving, IsMoving);
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
