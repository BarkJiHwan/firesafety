using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TutorialDataMgr : MonoBehaviour
{
    public static TutorialDataMgr Instance { get; private set; }
    [field: Header("플레이어별 상호작용 오브젝트 (0~5번)")]
    [field: SerializeField]
    public List<GameObject> InteractObjects { get; set; }

    [Header("플레이어별 튜토리얼 데이터 (0~5번)")]
    [SerializeField] private TutorialData[] _allTutorialData;

    [Header("기본 데이터 (에러 발생 시 사용)")]
    [SerializeField] private TutorialData _defaultData;

    public bool IsTriggerSupply { get; set; }

    public int PlayerNumber { get => _playerNumber; set => _playerNumber = value; }

    private int _playerNumber;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        IsTriggerSupply = false;
    }
    // 안전하게 가공된 플레이어 인덱스
    public void SetNumber(int num)
    {
        PlayerNumber = num;
    }

    // PlayerList 인덱스 기반 데이터 반환
    public TutorialData GetPlayerData(int playerListIndex)
    {
        if (_allTutorialData.Length == 0)
        {
            Debug.LogError("튜토리얼 데이터가 설정되지 않았습니다!");
            return _defaultData;
        }

        int index = Mathf.Clamp(playerListIndex, 0, _allTutorialData.Length - 1);

        if (_allTutorialData[index] == null)
        {
            Debug.LogError($"인덱스 {index}의 튜토리얼 데이터가 없습니다!");
            return _defaultData;
        }

        return _allTutorialData[index];
    }
    /// <summary>
    /// 플레이어 인덱스를 받아 배치 된 순서와 맞는 오브젝트 가져오기
    /// 씬에 배치 된 TutorialDataMgr의 오브젝트에 순서대로 넣어서 관리
    /// 코드적으로 문제가 없지만 확장과 변경이 불편함.
    /// 애초에 배치되어 있는 오브젝트를 활용했기 때문에 분편한 부분이 많음.
    /// </summary>
    public GameObject GetInteractObject(int playerListIndex)
    {
        if (playerListIndex < 0 || playerListIndex >= InteractObjects.Count)
        { return null; }
        return InteractObjects[playerListIndex];
    }
}
