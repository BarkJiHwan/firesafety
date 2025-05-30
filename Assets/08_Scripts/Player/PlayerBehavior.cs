using System.Collections;
using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;

public class PlayerBehavior : MonoBehaviour
{
    /* 플레이어의 상태 나타내는 변수 */
    private bool isSitting;
    private bool isGrabbing;
    private bool isMoving;

    public XROrigin playerOrigin;
    public Camera playerCam;
    public PhotonView photonView;

    //CHM - 소백이 관련
    [SerializeField] private GameObject sobaekPrefab;
    private GameObject sobaekInstance;

    public bool IsSitting => isSitting;
    public bool IsGrabbing => isGrabbing;
    public bool IsMoving => isMoving;

    // 내꺼 아니면 스크립트 자동으로 꺼지게하기
    private void Awake()
    {
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
        while (playerCam == null || playerOrigin == null)
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
            sobaekScript.Player = playerCam.transform;            
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

    private void LateUpdate()
    {
        Vector3 playerCamRot = playerCam.transform.rotation.eulerAngles;
        Vector3 currentRot = gameObject.transform.rotation.eulerAngles;
        Vector3 updatedRot = new Vector3(currentRot.x, playerCamRot.y, currentRot.z);

        gameObject.transform.rotation = Quaternion.Euler(updatedRot);
        gameObject.transform.position = playerOrigin.transform.position;
    }
}
