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

    private bool _hasAreaReset = false;
    private bool _hasRefreshedFireObjs = false;
    private bool _isInBurningPhase = false;
    private bool _hasLeaveDangerArea = false;

    [Header("태우리 생성 쿨타임")]
    [SerializeField] private float _isBuringCoolTime = 30;

    [Header("플레이어 수")]
    [Tooltip("추후 외부에서 주입 가능하도록 변경")]
    [SerializeField] private int _playerCount = 1;
    private GamePhase currentPhase;
    private WaitForSeconds _forSeconds;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshZoneDictionary();
    }
    private void Start()
    {
        currentPhase = GameManager.Instance.CurrentPhase;
        _isBuringCoolTime = _isBuringCoolTime / _playerCount;
        _forSeconds = new WaitForSeconds(_isBuringCoolTime);
    }
    void Update()
    {
        currentPhase = GameManager.Instance.CurrentPhase;

        if (currentPhase == GamePhase.Prevention && !_hasAreaReset)
        {
            Debug.Log("예방 페이즈 - 모든 구역 초기화");
            foreach (var zone in _zoneDict.Values)
            {
                ResetZone(zone);
            }
            _hasAreaReset = true;
        }

        if (currentPhase == GamePhase.Fire && !_hasRefreshedFireObjs)
        {
            Debug.Log("화재 페이즈 - 오브젝트 갱신");
            RefreshAllFireObjects();
            RefreshZoneDictionary();
            ListUpdateDictionary();
            ActivateRandomFireObjectsPerZone();
            StartCoroutine(ActivateTeawooriBurning());
            _hasRefreshedFireObjs = true;
        }

        if (currentPhase == GamePhase.Fever && !_isInBurningPhase)
        {
            Debug.Log("버닝 페이즈 - 태우리 쿨타임 감소");
            _isBuringCoolTime = _isBuringCoolTime / 2;
            _forSeconds = new WaitForSeconds(_isBuringCoolTime);
            _isInBurningPhase = true;
        }
        if (currentPhase == GamePhase.leaveDangerArea && !_hasLeaveDangerArea)
        {
            Debug.Log("대피페이즈 돌입. 일단 게임 종료");
            StopCoroutine(ActivateTeawooriBurning());
            Debug.Log("코루틴 멈춤");
            _hasLeaveDangerArea = true;
        }
    }

    // 모든 구역 초기화
    private void ResetZone(MapIndex zone)
    {
        foreach (var preventable in zone.FirePreventables)
        {
            preventable.IsFirePreventable = false;
        }
    }
    //딕셔너리 초기화
    public void RefreshZoneDictionary()
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
    //딕셔너리 값으로 리스트 업데이트
    public void ListUpdateDictionary()
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
    //딕셔너리에서 화재가 날 수 있는 오브젝트를 체크한 뒤 태우리 생성 가능상태로 변경시켜주는 코루틴
    private IEnumerator ActivateTeawooriBurning()
    {
        while (currentPhase == GamePhase.Fire || currentPhase == GamePhase.Fever)
        {
            // 현재 비활성화된 오브젝트 수집
            List<FireObjScript> inactiveFires = _fireObjList
                .Where(fire => !fire.IsBurning)
                .ToList();

            // 랜덤 선택 및 활성화
            if (inactiveFires.Count > 0)
            {
                int randomIndex = Random.Range(0, inactiveFires.Count);
                FireObjScript fireObj = inactiveFires[randomIndex];
                fireObj.IsBurning = true;

                Debug.Log($"활성화: {fireObj.name}");
                // 태우리 생성은 IsBurning setter에서 자동으로 처리됨
            }
            else
            {
                Debug.Log("비활성화된 오브젝트 없음");
                yield return null;
            }

            // 다음 활성화까지 대기
            yield return _forSeconds;
        }
    }
    private void ActivateRandomFireObjectsPerZone()
    {
        int totalTargetCount = _playerCount * 3;
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
