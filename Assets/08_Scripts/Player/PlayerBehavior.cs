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
    public PhotonView photonView;

    private Camera playerCam;
    private GameObject playerCamOffset;

    //CHM - 소백이 관련
    [SerializeField] private GameObject sobaekPrefab;
    private GameObject sobaekInstance;
    private bool hasSubscribedToGameManager = false;

    public bool IsSitting => isSitting;
    public bool IsGrabbing => isGrabbing;
    public bool IsMoving => isMoving;

    // 내꺼 아니면 스크립트 자동으로 꺼지게하기
    private void Awake()
    {
        playerCam = playerOrigin.Camera;
        playerCamOffset = playerOrigin.CameraFloorOffsetObject;

        if (!photonView.IsMine)
        {
            this.enabled = false;
            return;
        }
    }

    //CHM - 내 플레이어일 때만 게임 시작 이벤트 구독
    private void Start()
    {
        if (photonView.IsMine)
        {
            SubscribeToGameManager();
        }
    }

    //CHM - 게임 매니저 이벤트 구독
    private void SubscribeToGameManager()
    {
        if (hasSubscribedToGameManager)
            return;

        // GameManager가 준비될 때까지 대기 후 구독
        StartCoroutine(WaitForGameManagerAndSubscribe());
    }

    //CHM - GameManager 준비되면 이벤트 구독
    private IEnumerator WaitForGameManagerAndSubscribe()
    {
        // GameManager 인스턴스가 준비될 때까지 대기
        while (GameManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // 게임 시작 상태 확인 및 이벤트 구독
        if (GameManager.Instance.IsGameStart)
        {
            // 이미 게임이 시작된 상태라면 즉시 소백이 생성
            CreateSobaekWhenReady();
        }


        hasSubscribedToGameManager = true;

        // 게임 상태 주기적으로 체크 (임시 방법)
        StartCoroutine(CheckGameStartPeriodically());
    }

    //CHM - 게임 시작 상태 주기적 체크 (임시 방법)
    private IEnumerator CheckGameStartPeriodically()
    {
        bool lastGameStartState = false;

        while (this != null && gameObject.activeInHierarchy)
        {
            if (GameManager.Instance != null)
            {
                bool currentGameStartState = GameManager.Instance.IsGameStart;

                // 게임 시작 상태가 변경되었을 때
                if (currentGameStartState != lastGameStartState)
                {
                    if (currentGameStartState)
                    {
                        // 게임 시작됨 - 소백이 생성
                        OnGameStart();
                    }
                    else
                    {
                        // 게임 종료됨 - 소백이 비활성화
                        OnGameEnd();
                    }

                    lastGameStartState = currentGameStartState;
                }
            }

            yield return new WaitForSeconds(0.5f); // 0.5초마다 체크
        }
    }

    //CHM - 게임 시작 시 호출
    private void OnGameStart()
    {

        CreateSobaekWhenReady();
    }
    //CHM - 게임 종료 시 호출
    private void OnGameEnd()
    {

        if (sobaekInstance != null)
        {
            Sobaek sobaekScript = sobaekInstance.GetComponent<Sobaek>();
            if (sobaekScript != null)
            {
                sobaekScript.SetSobaekActive(false);
            }
        }
    }

    //CHM - 플레이어 준비되면 소백이 생성
    private void CreateSobaekWhenReady()
    {
        // 이미 소백이가 있다면 활성화만
        if (sobaekInstance != null)
        {
            Sobaek sobaekScript = sobaekInstance.GetComponent<Sobaek>();
            if (sobaekScript != null)
            {
                sobaekScript.SetSobaekActive(true);
            }
            return;
        }

        StartCoroutine(CreateSobaekCoroutine());
    }

    //CHM - 소백이 생성 코루틴
    private IEnumerator CreateSobaekCoroutine()
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

            // 게임이 시작된 상태라면 활성화, 아니면 비활성화
            bool shouldActivate = GameManager.Instance != null && GameManager.Instance.IsGameStart;
            sobaekScript.SetSobaekActive(shouldActivate);
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

        if (playerOrigin.RequestedTrackingOriginMode == XROrigin.TrackingOriginMode.Floor)
        {
            Vector3 currentFloorPos = playerCam.transform.position - playerCamOffset.transform.position;
            gameObject.transform.position = currentFloorPos;
        }
        else
        {
            gameObject.transform.position = playerOrigin.transform.position;
        }
    }
}
