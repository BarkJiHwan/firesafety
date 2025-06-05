using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialDataMgr : MonoBehaviour
{
    public static TutorialDataMgr Instance { get; private set; }

    [Header("플레이어별 튜토리얼 데이터 (0~5번)")]
    [SerializeField] private TutorialData[] allPlayerData;

    [Header("기본 데이터 (에러 발생 시 사용)")]
    [SerializeField] private TutorialData fallbackData;
    [Header("플레이어별 상호작용 오브젝트")]
    public List<GameObject> interactObjects; // 0~5번 인덱스에 플레이어별 오브젝트
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public TutorialData GetPlayerData(int actorNumber)
    {
        if (allPlayerData.Length == 0)
        {
            Debug.LogError("튜토리얼 데이터가 설정되지 않았습니다!");
            return fallbackData;
        }

        int index = Mathf.Clamp(
            (actorNumber - 1) % allPlayerData.Length,
            0,
            allPlayerData.Length - 1
        );

        if (allPlayerData[index] == null)
        {
            Debug.LogError($"인덱스 {index}의 튜토리얼 데이터가 없습니다!");
            return fallbackData;
        }

        return allPlayerData[index];
    }
    public GameObject GetInteractObject(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= interactObjects.Count)
            return null;
        return interactObjects[playerIndex];
    }
}
