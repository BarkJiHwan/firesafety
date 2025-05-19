using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireObjMgr : MonoBehaviour
{
    private static FireObjMgr _instance;
    public static FireObjMgr Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.Log("인스턴스 없음");
            }
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }

    public Dictionary<int, MapIndex> _zoneDict = new Dictionary<int, MapIndex>();

    private bool hasInitializedZones = false;
    private bool hasRefreshedFireObjs = false;
    private bool hasEnteredBurningPhase = false;

    [SerializeField] private int playerCount = 1; // 추후 외부에서 주입 가능하도록
    [SerializeField] private int firePerPlayer = 3;
    [SerializeField] private int minFiresPerZone = 1;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        //씬 변환이 있다면 사용
        //SceneManager.sceneLoaded += OnSceneLoaded;
        DontDestroyOnLoad(gameObject);
        RefreshZoneDict();
    }
    void Update()
    {
        var currentPhase = GameManager.Instance.CurrentPhase;

        if (currentPhase == GameManager.GamePhase.Prevention && !hasInitializedZones)
        {
            Debug.Log("예방 페이즈 - 모든 구역 초기화");
            foreach (var zone in _zoneDict.Values)
            {
                InitializeZone(zone);
            }
            hasInitializedZones = true;
        }

        if (currentPhase == GameManager.GamePhase.Fire && !hasRefreshedFireObjs)
        {
            Debug.Log("화재 페이즈 - 오브젝트 갱신");
            RefreshAllFireObjects();
            ActivateRandomFireObjectsPerZone();
            hasRefreshedFireObjs = true;
        }

        if (currentPhase == GameManager.GamePhase.Burning && !hasEnteredBurningPhase)
        {
            Debug.Log("버닝 페이즈 - TODO: 추가 로직");
            hasEnteredBurningPhase = true;
        }
    }
    
    ////씬 변환이 있다면 사용
    //void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    //{
    //    RefreshZoneDict();
    //}
    public void RefreshZoneDict()
    {
        _zoneDict.Clear();
        var zones = FindObjectsByType<MapIndex>(FindObjectsSortMode.None);
        foreach (var zone in zones)
        {
            if (_zoneDict.ContainsKey(zone.MapIndexValue))
            {
                Debug.Log($"딕셔너리 값 중복 있음. {zone.MapIndexValue}, {zone.name} 확인");
                continue;
            }
            _zoneDict.Add(zone.MapIndexValue, zone);
        }
    }

    // 모든 구역 초기화
    private void InitializeZone(MapIndex zone)
    {
        //foreach (var fireObj in zone.FireObjects)
        //{
        //    fireObj.IsBurning = false;
        //}
        foreach (var preventable in zone.FirePreventables)
        {
            preventable.IsFirePreventable = false;
        }
    }

    // 모든 구역의 화재 오브젝트 갱신
    public void RefreshAllFireObjects()
    {
        foreach (var zone in _zoneDict.Values)
        {
            foreach (var preventable in zone.FirePreventables)
            {
                if (preventable.IsFirePreventable)
                {
                    var fireObj = preventable.GetComponent<FireObjScript>();
                    if (fireObj != null)
                    {
                        zone.FireObjects.Remove(fireObj);
                    }
                }
            }
        }
    }
    private void ActivateRandomFireObjectsPerZone()
    {
        int totalTargetCount = minFiresPerZone + (playerCount * firePerPlayer);

        foreach (var zone in _zoneDict.Values)
        {
            var fireList = zone.FireObjects;

            if (fireList.Count < totalTargetCount)
            {
                Debug.Log($"구역 {zone.name}는 활성화 대상 수 부족 (보유: {fireList.Count}, 필요: {totalTargetCount}) - 스킵");
                continue;
            }

            // 리스트 복사 후 셔플
            var shuffled = new List<FireObjScript>(fireList);
            ShuffleList(shuffled);

            // 원하는 개수만큼 활성화
            for (int i = 0; i < totalTargetCount; i++)
            {
                shuffled[i].IsBurning = true;
            }
            Debug.Log($"구역 {zone.name}에 불 붙인 오브젝트 수: {totalTargetCount}");
        }
    }

    // 리스트 셔플 함수
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n - 1; i++)
        {
            int rand = Random.Range(i, n);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}
