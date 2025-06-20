using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonConnectManager : MonoBehaviourPunCallbacks
{
    private string _gameVersion = "1";
    [SerializeField] private PlayerSpawner _playerSpawner;

    private void Start()
    {
        /* 네트워크 Instantiate 할 프리팹 풀 초기화 */
        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;
        if (pool != null)
        {
            foreach (var soCharacter in _playerSpawner.playerCharacterArray)
            {
                if (pool.ResourceCache.ContainsKey(soCharacter.characterName))
                {
                    continue;
                }
                pool.ResourceCache.Add(soCharacter.characterName, soCharacter.characterPrefab);
            }
        }
        TestConnectPhotonServer();
    }

    // 테스트용 코드 덩어리들, 마스터 접속 상태인지 확인 후 접속 & 방 만들기 까지
    public void TestConnectPhotonServer()
    {
        PhotonNetwork.GameVersion = _gameVersion;
        PhotonNetwork.NickName = "테스트" + Random.Range(0, 1000);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            if (!PhotonNetwork.InRoom)
            {
                JoinRandomRoomOrCreatRoom();
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        JoinRandomRoomOrCreatRoom();
    }

    // 테스트용, 이럴 일 없겠지만 누군가 방에 참가했을 때
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            Debug.Log("player" + player.ActorNumber + " : " + player.NickName);
        }

        Debug.Log("roomName : " + PhotonNetwork.CurrentRoom.Name);
        Debug.Log("currentPlayers : " + PhotonNetwork.PlayerList.Length);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer.TagObject != null)
        {
            ((GameObject)otherPlayer.TagObject).SetActive(false);
            Destroy((GameObject)otherPlayer.TagObject);
            PhotonNetwork.Disconnect();
        }
    }

    /* 테스트용 방 곧바로 입장시, 바로 플레이어 생성이후 XR 컴포넌트 켜줌. */
    public override void OnJoinedRoom()
    {
        GameObject player = _playerSpawner.NetworkInstantiate(PlayerEnum.Jennie);
        player.GetComponent<PlayerComponents>().xRComponents.SetActive(true);

        GameManager.Instance.ResetGameTimer();
        Debug.Log("나 참가 " + PhotonNetwork.LocalPlayer + "Room : " + PhotonNetwork.CurrentRoom.Name);
        TutorialDataMgr.Instance.StartTutorial();
    }

    private void JoinRandomRoomOrCreatRoom()
    {/*, PlayerTtl = 0*/
        RoomOptions options = new RoomOptions { MaxPlayers = 6, IsOpen = true };
        PhotonNetwork.JoinRandomOrCreateRoom(
            null, // 랜덤 조건: 아무 조건 없음
            6, // 최대 인원 수 (RoomOptions에도 지정해두면 안전)
            MatchmakingMode.FillRoom, // 기존 방을 최대한 채우는 방식
            null, // 로비 타입 (null: 기본)
            null, // 추가 필터 (없음)
            null, // 방 이름 (null: 자동 생성)
            options // 방 옵션
        );
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        switch (returnCode)
        {
            case 32765:
                Debug.Log("방이 꽉 찼습니다.");
                break;
            case 32764:
                Debug.Log("방이 닫혔습니다.");
                break;
            default:
                Debug.Log("방 참여 실패, code : " + returnCode + " msg : " + message);
                break;
        }
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        switch (returnCode)
        {
            case 32765:
                Debug.Log("방이 꽉 찼습니다.");
                break;
            case 32764:
                Debug.Log("방이 닫혔습니다.");
                break;
            default:
                Debug.Log("방 참여 실패, code : " + returnCode + " msg : " + message);
                break;
        }
    }
}
