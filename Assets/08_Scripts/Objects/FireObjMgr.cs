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

    // 영역(Zone) 관련 데이터 저장용 딕셔너리
    public Dictionary<int, MapIndex> _zoneDict = new Dictionary<int, MapIndex>(); // 각 영역의 인덱스와 MapIndex 매핑

    // 현재 맵에 존재하는 모든 화재 오브젝트 리스트
    public List<FireObjScript> _fireObjList = new List<FireObjScript>(); // 맵 내의 화재 오브젝트 참조 리스트

    // 화재 예방이 가능한 오브젝트 리스트
    public List<FirePreventable> _preventObjList = new List<FirePreventable>(); // 화재 예방이 가능한 오브젝트 리스트

    // 상태 플래그 변수들
    private bool _hasAreaReset = false;            // 영역이 리셋되었는지 여부
    private bool _hasRefreshedFireObjs = false;    // 화재 오브젝트가 갱신되었는지 여부
    private bool _isInBurningPhase = false;        // 현재 화재(불타는) 단계인지 여부
    private bool _hasLeaveDangerArea = false;      // 위험 구역을 벗어났는지 여부

    // 태우리 생성 쿨타임 관련 변수
    [Header("태우리 생성 쿨타임")]
    [SerializeField] private float _isBuringCoolTime = 30; // 태우리 생성 쿨타임(초 단위)

    // 플레이어 수 관련 변수
    [Header("플레이어 수")]
    [Tooltip("추후 외부에서 주입 가능하도록 변경")]
    [SerializeField] private int _playerCount = 1; // 현재 플레이어 수(기본값 1명)

    // 게임의 현재 단계(페이즈)
    private GamePhase currentPhase; // 현재 게임 페이즈

    // 코루틴에서 사용할 대기 시간 객체
    private WaitForSeconds _forSeconds; // 코루틴에서 대기 시간으로 사용

    // 카운트(예: 남은 오브젝트 수 등)
    [SerializeField] private int _count; // 카운트 변수(용도에 따라 이름 구체화 권장)

    // 화재 예방 점수(외부에서 읽기만 가능)
    [field: SerializeField]
    public int CompletedPreventionScore { get; private set; } // 완료된 화재 예방 점수

    // 현재 점수(외부에서 읽기만 가능)
    [field: SerializeField]
    public int _score { get; private set; } // 현재 점수

    // 카운트 프로퍼티(외부에서 읽고 쓸 수 있음)
    public int Count { get => _count; set => _count = value; } // _count의 Getter/Setter

    // 점수 관리 객체
    [SerializeField] private ScoreManager _scoreManager; // 점수 관리용 ScoreManager 참조

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
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
            // 구역 초기화
            foreach (var zone in _zoneDict.Values)
            {
                ResetZone(zone);
            }
            RefreshAllPrevention(); // 모든 화재 예방 오브젝트 상태를 갱신
            StartCoroutine(PreventionPhaseCoroutine());
            _hasAreaReset = true;
        }
        if (currentPhase == GamePhase.Fire && !_hasRefreshedFireObjs)
        {
            if(_scoreManager == null)
            {
                _scoreManager = FindObjectOfType<ScoreManager>();
            }
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
            _isBuringCoolTime = _isBuringCoolTime / 2; // 버닝 타임이면 태우리 생성 쿨타임 반으로 감소
            _forSeconds = new WaitForSeconds(_isBuringCoolTime);
            _isInBurningPhase = true;
        }
        if (currentPhase == GamePhase.LeaveDangerArea && !_hasLeaveDangerArea)
        {
            StopAllCoroutines(); // 코루틴 멈추기
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
    // 딕셔너리 초기화
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

    // 예방이 안된 오브젝트 갱신
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
    // 모든 화재 예방 오브젝트 상태를 갱신
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
    // 구역내에 최소 1개의 오브젝트먼저 불이나게 구현
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
    //예방 점수 측정
    public void CompletedPreventionPhase()
    {
        CompletedPreventionScore = CalculateScore(_playerCount, Count);
        _scoreManager.SetScore(ScoreType.Prevention_Count, CompletedPreventionScore);
    }

    // 들어와 있는 사람 수대로 점수 측정
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
    // 예방 시간에 따른 점수 측정 코루틴
    private IEnumerator PreventionPhaseCoroutine()
    {
        float _timer = Time.time;

        // 모든 오브젝트가 예방될 때까지 대기
        yield return new WaitUntil(() => Count >= _preventObjList.Count || currentPhase == GamePhase.Fire);

        float elapsedTime = Time.time - _timer;
        int calculatedScore = CalculateScore(elapsedTime, _playerCount);
        _score = calculatedScore;
        _scoreManager.SetScore(ScoreType.Prevention_Time, calculatedScore);
    }
    // 예방 시간 점수 측정
    private int CalculateScore(float time, int players)
    {
        players = Mathf.Clamp(players, 1, 6);
        if (players == 1)
            return 25; // 혼자 할 때는 항상 만점
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
