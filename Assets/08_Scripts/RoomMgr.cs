using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using Unity.XR.OpenVR;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RoomMgr : MonoBehaviourPunCallbacks
{
    private DialogueLoader _dialogueLoader;
    private DialoguePlayer _dialoguePlayer;

    private void Start()
    {
        GameObject dialogue = null;
        if(_dialogueLoader == null)
        {
            _dialogueLoader = FindObjectOfType<DialogueLoader>();
            dialogue = _dialogueLoader.gameObject;
        }

        if(_dialoguePlayer == null)
        {
            _dialoguePlayer = dialogue.GetComponent<DialoguePlayer>();
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (PhotonNetwork.IsMasterClient && changedProps.ContainsKey("IsReady"))
        {
            CheckAllPlayersReady();
        }
    }
    private void CheckAllPlayersReady()
    {
        if (isAllPlayersReady())
        {
            //Tutorial_NAR_010번 나레이션 실행 : 이제 게임 할거니까 잠깐 기다려~
            _dialoguePlayer.PlayWithText("TUT_010", UIType.Narration);
            _dialoguePlayer.onFinishDialogue += CallRPCToPlayers;
        }
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CheckAllPlayersReady();
        }
    }

    private void CallRPCToPlayers()
    {
        _dialoguePlayer.onFinishDialogue -= CallRPCToPlayers;
        photonView.RPC("StartGameCountdown", RpcTarget.All);
    }

    public bool isAllPlayersReady()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("IsReady") ||
                !(bool)player.CustomProperties["IsReady"])
                return false;
        }

        return true;
    }

    [PunRPC]
    private IEnumerator StartGameCountdown()
    {
        int prevCount = -1;
        float timer = 3f;
        while (timer > 0f)
        {
            int currentCount = Mathf.CeilToInt(timer);
            if (currentCount != prevCount)
            {
                prevCount = currentCount;
            }
            timer -= Time.deltaTime;
            yield return null;
        }
        StartGame();
    }

    private void StartGame()
    {
        // 게임 시작 시 룸 영구 잠금
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable()
            {
                //{ "isLocked", true },
                { "gameStarted", true }
            });

            // 타이머 동기화 시작
            StartCoroutine(SyncTimerRoutine());

            // 게임 종료 이벤트 구독
            GameManager.Instance.OnGameEnd += OnGameEndHandler;
        }

        // 게임 시작
        GameManager.Instance.GameStartWhenAllReady();
    }

    // 타이머 동기화 (마스터 클라이언트만)
    private IEnumerator SyncTimerRoutine()
    {
        WaitForSeconds secods = new WaitForSeconds(0.5f);
        while (GameManager.Instance != null)
        {
            photonView.RPC("SyncTimer", RpcTarget.All, GameManager.Instance.GameTimer);
            yield return secods;
        }
    }

    [PunRPC]
    void SyncTimer(float time)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameTimer(time);
        }
    }
    // 게임 종료
    private void OnGameEndHandler()
    {
        Debug.Log("게임이 종료되었습니다.");
        if (PhotonNetwork.IsMasterClient)
        {
            GameManager.Instance.IsGameStart = false;
            GameManager.Instance.OnGameEnd -= OnGameEndHandler;
        }
        //모든 코루틴 종료
        StopAllCoroutines();
    }
}
