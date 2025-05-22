using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonConnectManager : MonoBehaviourPunCallbacks
{
    private string _gameVersion = "1";
    private string _testRoomName = "testtest123";
    private string _testLobbyName = "scTestLobby";

    [SerializeField] private GameManager _gameManager;
    [SerializeField] private PlayerSpawner _playerSpawner;

    private void Awake()
    {
        _gameManager = FindObjectOfType<GameManager>();
    }

    private void Start()
    {
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
                PhotonNetwork.JoinOrCreateRoom(
                    _testRoomName,
                    new RoomOptions { MaxPlayers = 6 },
                    new TypedLobby(_testLobbyName, LobbyType.Default)
                );
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        if (!PhotonNetwork.InRoom)
        {
            PhotonNetwork.JoinOrCreateRoom(
                _testRoomName,
                new RoomOptions{ MaxPlayers = 6 },
                new TypedLobby(_testLobbyName, LobbyType.Default)
            );
        }
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
        Debug.Log("플레이어 나감 : " + otherPlayer.NickName);
        Debug.Log(otherPlayer.TagObject);

        if (PhotonNetwork.IsMasterClient)
        {
            // _gameManager.RemoveReadyPlayer(otherPlayer);
        }

        if (otherPlayer.TagObject != null)
        {
            ((GameObject)otherPlayer.TagObject).SetActive(false);
            Destroy((GameObject)otherPlayer.TagObject);
        }
    }

    /* 테스트용 방 곧바로 입장시, 바로 카트 생성해준다. */
    public override void OnJoinedRoom()
    {
        Debug.Log("I'm Joined, " + PhotonNetwork.LocalPlayer + "Room : " + PhotonNetwork.CurrentRoom.Name);
        // _gameManager.InstantiateObject();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Lobby : " + PhotonNetwork.CurrentLobby.Name);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("방 참여 실패, code : " + returnCode + " msg : " + message);
    }
}
