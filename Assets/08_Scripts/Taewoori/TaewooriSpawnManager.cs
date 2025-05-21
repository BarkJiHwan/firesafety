//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class TaewooriSpawnManager : MonoBehaviour
//{
//    [Header("태우리 설정")]
//    [Tooltip("태우리가 파괴된 후 재생성되는 시간 간격 (초)")]
//    [SerializeField] private float _respawnCoolTime = 10f;

//    private float _currentRespawnCoolTime;
//    private TaewooriPoolManager _poolManager;

//    // 태우리 상태 추적
//    private Dictionary<FireObjScript, bool> _hasTaewoori = new Dictionary<FireObjScript, bool>();
//    private Dictionary<FireObjScript, float> _respawnTimers = new Dictionary<FireObjScript, float>();

//    // 게임 매니저 버닝 페이즈에 따라 자동으로 결정
//    public bool IsFeverTime => GameManager.Instance != null &&
//                             GameManager.Instance.CurrentPhase == GameManager.GamePhase.Burning;

//    private void Awake()
//    {
//        _poolManager = FindObjectOfType<TaewooriPoolManager>();
//        if (_poolManager == null)
//        {
//            Debug.LogWarning("[TaewooriSpawnManager] TaewooriPoolManager를 찾을 수 없습니다!");
//        }

//        UpdateRespawnCoolTime();
//        // 태우리 이벤트 구독
//        Taewoori.OnTaewooriSpawned += HandleTaewooriSpawned;
//        Taewoori.OnTaewooriDestroyed += HandleTaewooriDestroyed;

//        Debug.Log("<color=cyan>[TaewooriSpawnManager] 이벤트 구독 완료</color>");

//    }
//    private void Start()
//    {
//        RegisterAllFireObjectsInScene();
//        Debug.Log("<color=cyan>[TaewooriSpawnManager] 모든 화재 오브젝트 등록 완료</color>");
//    }

//    private void Update()
//    {
//        // 피버타임에 따라 리스폰 쿨타임 업데이트
//        UpdateRespawnCoolTime();

//        // 리스폰 타이머 업데이트
//        UpdateRespawnTimers();
//    }

//    private void UpdateRespawnCoolTime()
//    {
//        // 버닝 페이즈(피버타임)이면 쿨타임 50% 감소
//        _currentRespawnCoolTime = IsFeverTime ? _respawnCoolTime * 0.5f : _respawnCoolTime;
//    }

//    private void UpdateRespawnTimers()
//    {
//        List<FireObjScript> finishedTimers = new List<FireObjScript>();

//        // 딕셔너리 키를 복사하여 안전하게 순회
//        List<FireObjScript> timerKeys = new List<FireObjScript>(_respawnTimers.Keys);


//        // 모든 리스폰 타이머 업데이트
//        foreach (var fireObj in _respawnTimers.Keys)
//        {
//            if (fireObj == null)
//                continue;

//            // 타이머 업데이트
//            _respawnTimers[fireObj] += Time.deltaTime;

//            // 리스폰 시간이 되었고, 화재 오브젝트가 활성화되어 있으면
//            if (_respawnTimers[fireObj] >= _currentRespawnCoolTime && fireObj.IsBurning)
//            {
//                // 태우리 생성
//                SpawnTaewooriAtFireObj(fireObj);
//                finishedTimers.Add(fireObj);
//            }
//        }

//        // 완료된 타이머 제거
//        foreach (var fireObj in finishedTimers)
//        {
//            _respawnTimers.Remove(fireObj);
//        }
//    }

//    // 화재 오브젝트에 태우리 생성
//    private void SpawnTaewooriAtFireObj(FireObjScript fireObj)
//    {
//        if (_poolManager == null || fireObj == null || !fireObj.IsBurning)
//            return;

//        // 이미 태우리가 있으면 생성하지 않음
//        if (_hasTaewoori.TryGetValue(fireObj, out bool hasTaewoori) && hasTaewoori)
//        {
//            Debug.Log($"[TaewooriSpawnManager] {fireObj.name}에 이미 태우리가 있어 생성을 건너뜁니다.");
//            return;
//        }

//        // 예방 완료된 오브젝트는 태우리 생성 안함
//        FirePreventable preventable = fireObj.GetComponent<FirePreventable>();
//        if (preventable != null && preventable.IsFirePreventable)
//            return;

//        // 스폰 위치 계산
//        Vector3 spawnPos = fireObj.TaewooriPos();

//        // 태우리 생성
//        var taewooriObj = _poolManager.SpawnTaewooriAtPosition(spawnPos, fireObj);

//        if (taewooriObj != null)
//        {
//            // 태우리 생성 상태 업데이트
//            _hasTaewoori[fireObj] = true;
//            Debug.Log($"[TaewooriSpawnManager] {fireObj.name}에 태우리 생성됨. 피버타임: {IsFeverTime}");
//        }
//    }

//    // 태우리가 파괴되었을 때 호출
//    public void NotifyTaewooriDestroyed(FireObjScript fireObj)
//    {
//        if (fireObj == null)
//            return;

//        // 태우리 상태 업데이트
//        _hasTaewoori[fireObj] = false;

//        // 리스폰 타이머 시작 (불이 켜져 있을 때만)
//        if (fireObj.IsBurning)
//        {
//            _respawnTimers[fireObj] = 0f;
//            Debug.Log($"<color=red>[TaewooriSpawnManager] {fireObj.name}의 태우리가 파괴됨, 리스폰 타이머 시작</color>");
//        }
//    }

//    // FireObjScript 등록 및 이벤트 구독
//    public void RegisterFireObject(FireObjScript fireObj)
//    {
//        if (fireObj == null || _hasTaewoori.ContainsKey(fireObj))
//            return;

//        // 초기 상태 설정
//        _hasTaewoori[fireObj] = false;

//        // 이미 불이 켜져 있으면 즉시 태우리 생성
//        if (fireObj.IsBurning)
//        {
//            SpawnTaewooriAtFireObj(fireObj);
//        }

//        // 이벤트 구독
//        fireObj.OnBurningStateChanged += OnFireObjBurningStateChanged;
//    }

//    // 화재 오브젝트 상태 변경 이벤트 핸들러
//    private void OnFireObjBurningStateChanged(FireObjScript fireObj, bool isBurning)
//    {
//        if (fireObj == null)
//            return;

//        if (isBurning)
//        {
//            // 불이 켜졌을 때 바로 태우리 생성 (이미 있지 않다면)
//            if (!_hasTaewoori.ContainsKey(fireObj) || !_hasTaewoori[fireObj])
//            {
//                if (!_respawnTimers.ContainsKey(fireObj))
//                {
//                    // 바로 생성
//                    SpawnTaewooriAtFireObj(fireObj);
//                }
//                // 리스폰 타이머가 있으면 그대로 진행
//            }
//        }
//        else
//        {
//            // 불이 꺼졌을 때 태우리 제거
//            if (_poolManager != null && _hasTaewoori.ContainsKey(fireObj) && _hasTaewoori[fireObj])
//            {
//                _poolManager.DeactivateTaewoories(fireObj);
//                _hasTaewoori[fireObj] = false;

//                // 타이머가 있다면 취소
//                if (_respawnTimers.ContainsKey(fireObj))
//                {
//                    _respawnTimers.Remove(fireObj);
//                }
//            }
//        }
//    }

//    // 씬의 모든 화재 오브젝트 등록 (필요시 호출)
//    public void RegisterAllFireObjectsInScene()
//    {
//        if (FireObjMgr.Instance == null)
//            return;

//        foreach (var zoneEntry in FireObjMgr.Instance._zoneDict)
//        {
//            var zone = zoneEntry.Value;
//            foreach (var fireObj in zone.FireObjects)
//            {
//                if (fireObj != null)
//                {
//                    RegisterFireObject(fireObj);
//                }
//            }
//        }
//    }

//    private void OnDestroy()
//    {
//        // 이벤트 구독 해제
//        Taewoori.OnTaewooriSpawned -= HandleTaewooriSpawned;
//        Taewoori.OnTaewooriDestroyed -= HandleTaewooriDestroyed;

//        // FireObjScript 이벤트 구독 해제
//        foreach (var fireObj in _hasTaewoori.Keys)
//        {
//            if (fireObj != null)
//            {
//                fireObj.OnBurningStateChanged -= OnFireObjBurningStateChanged;
//            }
//        }
//    }

//    // 태우리 생성 이벤트 핸들러
//    private void HandleTaewooriSpawned(Taewoori taewoori, FireObjScript fireObj)
//    {
//        if (fireObj != null)
//        {
//            _hasTaewoori[fireObj] = true;
//            Debug.Log($"<color=green>[TaewooriSpawnManager] 태우리 생성 감지: {taewoori.name}, 소스: {fireObj.name}</color>");
//        }
//    }

//    // 태우리 파괴 이벤트 핸들러
//    private void HandleTaewooriDestroyed(Taewoori taewoori, FireObjScript fireObj)
//    {
//        if (fireObj != null)
//        {
//            _hasTaewoori[fireObj] = false;

//            // 리스폰 타이머 시작 (불이 켜져 있을 때만)
//            if (fireObj.IsBurning)
//            {
//                _respawnTimers[fireObj] = 0f;
//                Debug.Log($"<color=red>[TaewooriSpawnManager] 태우리 파괴 감지: {taewoori.name}, 소스: {fireObj.name}, 리스폰 타이머 시작</color>");
//            }
//        }
//    }
//}
