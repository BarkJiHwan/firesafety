using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RoomMgr : MonoBehaviourPunCallbacks
{
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (PhotonNetwork.IsMasterClient && changedProps.ContainsKey("IsReady"))
        {
            CheckAllPlayersReady();
        }
    }
    private void CheckAllPlayersReady()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("IsReady") ||
                !(bool)player.CustomProperties["IsReady"])
                return;
        }
        Debug.Log("모두 준비 됐으니 게임 시작합니다");
        // 모든 플레이어 준비 완료 → 3초 카운트다운 시작
        photonView.RPC("StartGameCountdown", RpcTarget.All);
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
        photonView.RPC("StartGame", RpcTarget.All);
    }

    [PunRPC]
    void StartGame()
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
            GameManager.Instance.OnGameEnd -= OnGameEndHandler;
        }
        //모든 코루틴 종료
        StopAllCoroutines();
    }
}
