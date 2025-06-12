using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

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
    public List<FirePreventable> _preventObjList = new List<FirePreventable>();

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
    [SerializeField] private int _count;

    [field: SerializeField]
    public int CompletedPreventionScore { get; private set; }
    [field: SerializeField]
    public int _score { get; private set; }
    public int Count { get => _count; set => _count = value; }

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
        _forSeconds = new WaitForSeconds(_isBuringCoolTime);
    }
    void Update()
    {
        currentPhase = GameManager.Instance.CurrentPhase;
        if (currentPhase == GamePhase.Prevention && !_hasAreaReset)
        {
            _playerCount = PhotonNetwork.PlayerList.Count();
            _isBuringCoolTime = _isBuringCoolTime / _playerCount;
            Debug.Log("예방 페이즈 - 모든 구역 초기화");
            foreach (var zone in _zoneDict.Values)
            {
                ResetZone(zone);
            }
            RefreshAllPrevention();
            StartCoroutine(PreventionPhaseCoroutine());
            _hasAreaReset = true;
        }
        if (currentPhase == GamePhase.Fire && !_hasRefreshedFireObjs)
        {
            Debug.Log("화재 페이즈 - 오브젝트 갱신");
            CompletedPreventionPhase(); //예방 점수 측정
            RefreshAllFireObjects(); //예방이 안된 오브젝트 갱신
            RefreshZoneDictionary(); //갱신한 값으로 딕셔너리 초기화
            ListUpdateDictionary(); //딕셔너리 값으로 화재오브젝트 리스트 업데이트
            ActivateRandomFireObjectsPerZone(); //구역내에 최소 1개의 오브젝트먼저 불이나게 구현
            StartCoroutine(ActivateTeawooriBurning()); //화재 코루틴 시작
            _hasRefreshedFireObjs = true;
        }

        if (currentPhase == GamePhase.Fever && !_isInBurningPhase)
        {
            Debug.Log("버닝 페이즈 - 태우리 쿨타임 감소");
            _isBuringCoolTime = _isBuringCoolTime / 2;
            _forSeconds = new WaitForSeconds(_isBuringCoolTime);
            _isInBurningPhase = true;
        }
        if (currentPhase == GamePhase.LeaveDangerArea && !_hasLeaveDangerArea)
        {
            Debug.Log("대피페이즈 돌입. 일단 게임 종료");
            StopAllCoroutines();
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
                preventable.SomkePrefabActiveOut();
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
    public void RefreshAllPrevention()
    {
        foreach (var zone in _zoneDict.Values)
        {
            foreach (var zonePreventObj in zone.FirePreventables)
            {
                var preventObj = zonePreventObj.GetComponent<FirePreventable>();
                _preventObjList.Add(preventObj);
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
    public void CompletedPreventionPhase()
    {
        CompletedPreventionScore = CalculateScore(_playerCount, Count);
    }
    public int CalculateScore(int playerCount, int count)
    {
        switch (playerCount)
        {
            case 1:
                if (count >= _preventObjList.Count)
                    return 25;
                else if (count >= 5)
                    return 20;
                else
                    return 15;
            case 2:
                if (count >= 5)
                    return 25;
                else if (count >= 3)
                    return 20;
                else
                    return 15;
            case 3:
                if (count >= 4)
                    return 25;
                else if (count >= 2)
                    return 20;
                else
                    return 15;
            case 4:
                if (count >= 3)
                    return 25;
                else if (count == 2)
                    return 20;
                else
                    return 15;
            case 5:
                if (count >= 2)
                    return 25;
                else if (count == 1)
                    return 20;
                else
                    return 15;
            case 6:
                if (count >= 2)
                    return 25;
                else if (count == 1)
                    return 20;
                else
                    return 15;
            default:
                return 0;
        }
    }

    private IEnumerator PreventionPhaseCoroutine()
    {
        float _timer = Time.time;

        // 모든 오브젝트가 예방될 때까지 대기
        yield return new WaitUntil(() => Count >= _preventObjList.Count || currentPhase == GamePhase.Fire);

        float elapsedTime = Time.time - _timer;
        int calculatedScore = CalculateScore(elapsedTime, _playerCount);
        _score += calculatedScore;

        Debug.Log($"모든 오브젝트 예방 완료! 경과 시간: {elapsedTime:F2}초, 점수: {_score}");
    }
    private int CalculateScore(float time, int players)
    {
        players = Mathf.Clamp(players, 1, 6);
        //if (players == 1)
        //    return 25; // 혼자 할 때는 항상 만점
        float maxScoreThreshold = Mathf.Round(60f / players);
        float midScoreThreshold = maxScoreThreshold + 10f;

        if (time <= maxScoreThreshold)
            return 25;
        else if (time <= midScoreThreshold)
            return 20;
        else
            return 15;
    }
}
