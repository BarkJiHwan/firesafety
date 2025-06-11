using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
public class TutorialDataMgr : MonoBehaviourPun
{
    public static TutorialDataMgr Instance { get; private set; }
    [field: Header("플레이어별 상호작용 오브젝트 (0~5번)")]
    [field: SerializeField]
    public List<GameObject> InteractObjects { get; set; }
    public bool _tutorialCompleted;
    [Header("플레이어별 튜토리얼 데이터 (0~5번)")]
    [SerializeField] private TutorialData[] allPlayerData;

    [Header("기본 데이터 (에러 발생 시 사용)")]
    [SerializeField] private TutorialData fallbackData;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        _tutorialCompleted = false;
    }

    // PlayerList 인덱스 기반 데이터 반환
    public TutorialData GetPlayerData(int playerListIndex)
    {
        if (allPlayerData.Length == 0)
        {
            Debug.LogError("튜토리얼 데이터가 설정되지 않았습니다!");
            return fallbackData;
        }

        int index = Mathf.Clamp(playerListIndex, 0, allPlayerData.Length - 1);

        if (allPlayerData[index] == null)
        {
            Debug.LogError($"인덱스 {index}의 튜토리얼 데이터가 없습니다!");
            return fallbackData;
        }

        return allPlayerData[index];
    }

    public GameObject GetInteractObject(int playerListIndex)
    {

        if (playerListIndex < 0 || playerListIndex >= InteractObjects.Count)
        { return null; }
        return InteractObjects[playerListIndex];

    }
    public void StartTutorial()
    {
        if (PhotonNetwork.LocalPlayer.IsLocal)
        {
            StartCoroutine(TutorialRoutine());
        }
    }

    private IEnumerator TutorialRoutine()
    {
        float timer = 3f;
        while (timer > 0f)
        {
            Debug.Log($"튜토리얼 준비까지: {timer:F1}초");
            timer -= Time.deltaTime;
            yield return null;
        }

        timer = 90f;
        while (timer > 0 && !_tutorialCompleted)
        {
            Debug.Log($"튜토리얼중 {timer:F1}초");
            timer -= Time.deltaTime;
            yield return null;
        }
        if(timer > 0f)
        {
            CompleteTutorial();
        }
        else
        {
            //시간 초과시 발생해야 하는 이벤트 등....을 여기서 추가하면 됨
            CompleteTutorial();
        }
    }
    public void IsTutorialComplete() => _tutorialCompleted = true;

    public void CompleteTutorial()
    {
        Hashtable props = new Hashtable() { { "IsReady", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
}
