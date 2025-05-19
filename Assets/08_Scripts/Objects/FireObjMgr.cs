using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public List<FireObjScript> _fireObjList = new List<FireObjScript>();

    private bool hasInitializedZones = false;
    private bool hasRefreshedFireObjs = false;
    private bool hasEnteredBurningPhase = false;

    private float _isBuringCoolTime = 30;
    [SerializeField] private int playerCount = 1; // 추후 외부에서 주입 가능하도록
    [SerializeField] private int firePerPlayer = 3;
    [SerializeField] private int minFiresPerZone = 1;

    [SerializeField] private List<FireObjScript> _activationQueue = new(); // 활성화 대기열
    [SerializeField] private bool _isProcessing = false; // 동시 실행 방지 플래그
    [SerializeField] private float _lastActivationTime = 0f;

    WaitForSeconds _forSeconds;

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
    private void Start()
    {
        _isBuringCoolTime = _isBuringCoolTime / playerCount;
        _forSeconds = new WaitForSeconds(_isBuringCoolTime);
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
            RefreshZoneDict();
            ListUpdateDic();
            ActivateRandomFireObjectsPerZone();
            StartCoroutine(태우리생성());
            hasRefreshedFireObjs = true;
        }

        if (currentPhase == GameManager.GamePhase.Burning && !hasEnteredBurningPhase)
        {
            Debug.Log("버닝 페이즈 - TODO: 추가 로직");
            _isBuringCoolTime = _isBuringCoolTime / playerCount / 2;
            _forSeconds = new WaitForSeconds(_isBuringCoolTime);
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
    public void ListUpdateDic()
    {
        foreach (var zone in _zoneDict.Values)
        {
            foreach (var zonefireObj in zone.FireObjects)
            {
                var fireObj = zonefireObj.GetComponent<FireObjScript>();
                _fireObjList.Add(fireObj);
            }
        }
    }
    private int _lastActivatedIndex = -1; // 마지막으로 활성화한 인덱스

    private IEnumerator 태우리생성()
    {
        while (true)
        {
            // 1. 현재 비활성화된 오브젝트 수집
            List<FireObjScript> inactiveFires = _fireObjList
                .Where(fire => !fire.IsBurning)
                .ToList();

            // 2. 랜덤 선택 및 활성화
            if (inactiveFires.Count > 0)
            {
                int randomIndex = Random.Range(0, inactiveFires.Count);
                inactiveFires[randomIndex].IsBurning = true;
                Debug.Log($"활성화: {inactiveFires[randomIndex].name}");
            }
            else
            {
                Debug.Log("비활성화된 오브젝트 없음");
            }

            // 3. 다음 활성화까지 대기 (인원수 / 30초)
            yield return _forSeconds;
        }
    }
    private void ActivateRandomFireObjectsPerZone()
    {
        int totalTargetCount = playerCount * 3;
        int currentBurningCount = 0;

        // 1. 각 구역에서 1개씩 불 붙이기 (단, 목표 초과하지 않도록)
        foreach (var zone in _zoneDict.Values)
        {
            if (currentBurningCount >= totalTargetCount)
                break;

            var fireList = zone.FireObjects;
            if (fireList.Count == 0)
            {
                Debug.Log($"구역 {zone.name}는 오브젝트 없음 - 스킵");
                continue;
            }

            var shuffled = new List<FireObjScript>(fireList);
            ShuffleList(shuffled);

            for (int i = 0; i < shuffled.Count; i++)
            {
                if (!shuffled[i].IsBurning)
                {
                    shuffled[i].IsBurning = true;
                    currentBurningCount++;
                    Debug.Log($"구역 {zone.name}에 최소 1개 불 붙임: {shuffled[i].name}");
                    break;
                }
            }
        }

        // 2. 남은 수만큼 전체에서 랜덤하게 선택하여 불 붙이기
        int remainingCount = totalTargetCount - currentBurningCount;
        if (remainingCount <= 0)
        {
            Debug.Log($"목표 개수({totalTargetCount})를 달성함.");
            return;
        }

        List<FireObjScript> allAvailable = _zoneDict.Values
            .SelectMany(zone => zone.FireObjects)
            .Where(obj => !obj.IsBurning)
            .ToList();

        if (allAvailable.Count < remainingCount)
        {
            Debug.LogWarning($"전체 불붙일 수 있는 오브젝트 수 부족 (필요: {remainingCount}, 보유: {allAvailable.Count})");
            remainingCount = allAvailable.Count;
        }

        ShuffleList(allAvailable);

        for (int i = 0; i < remainingCount; i++)
        {
            allAvailable[i].IsBurning = true;
            currentBurningCount++;
        }

        Debug.Log($"총 불 붙인 오브젝트 수: {currentBurningCount} / 목표: {totalTargetCount}");
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
