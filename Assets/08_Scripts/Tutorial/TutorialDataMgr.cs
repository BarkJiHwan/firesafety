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

    [Header("플레이어별 튜토리얼 데이터 (0~5번)")]
    [SerializeField] private TutorialData[] allPlayerData;

    [Header("기본 데이터 (에러 발생 시 사용)")]
    [SerializeField] private TutorialData fallbackData;

    private Coroutine _tutorialRoutine;

    public bool IsStartTutorial { get; set; }
    public bool IsTutorialFailed { get; set; }
    public float Timer { get; private set; } = 90f;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        IsStartTutorial = false;
        IsTutorialFailed = false;
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
            _tutorialRoutine = StartCoroutine(TutorialRoutine());
        }
    }

    private IEnumerator TutorialRoutine()
    {
        yield return new WaitUntil(() => IsStartTutorial);
        float timer = 90f;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        TutorialFailed();
    }

    public void TutorialFailed() => IsTutorialFailed = true;

    public void StopTutorialRoutine() => StopCoroutine(_tutorialRoutine);
}
