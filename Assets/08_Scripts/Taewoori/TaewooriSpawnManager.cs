using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FireObjectEntry
{
    [Header("화재 오브젝트 참조")]
    [Tooltip("태우리를 생성할 FireObjScript 컴포넌트가 있는 게임 오브젝트")]
    public FireObjScript fireObject;

    [Header("불 상태 설정")]
    [Tooltip("체크하면 이 오브젝트에서 태우리가 생성됩니다")]
    public bool isTaewooriOn;
}

public class TaewooriSpawnManager : MonoBehaviour
{
    #region 변수 선언
    [Header("전역 태우리 설정")]
    [Tooltip("모든 화재 오브젝트의 태우리 활성화 여부를 한 번에 제어")]
    [SerializeField] private bool _isAllfire = false;

    [Header("태우리 생성 설정")]
    [Tooltip("태우리가 처음 생성되는 시간 간격 (초)")]
    [SerializeField] private float _spawnCoolTime = 10f;

    [Tooltip("태우리가 파괴된 후 재생성되는 시간 간격 (초)")]
    [SerializeField] private float _respawnCoolTime = 10f;

    [Header("피버타임 설정")]
    [Tooltip("체크하면 피버타임 모드가 활성화됩니다.")]
    [SerializeField] private bool _isPeverTime = false;

    [Header("화재 오브젝트 목록")]
    [Tooltip("관리할 화재 오브젝트들과 각각의 설정")]
    [SerializeField] private List<FireObjectEntry> _managedFireObjectsList = new List<FireObjectEntry>();

    private float _currentSpawnCoolTime;
    private float _currentRespawnCoolTime;
    private bool _wasFeverTime = false;
    private GameManager.GamePhase _lastPhase;
    private TaewooriPoolManager _poolManager;
    private bool _autoRegisterAllOnStart = true;

    // 내부 상태 관리
    private Dictionary<FireObjScript, FireObjectEntry> _managedFireObjects = new Dictionary<FireObjScript, FireObjectEntry>();
    private Dictionary<FireObjScript, float> _spawnTimers = new Dictionary<FireObjScript, float>();
    private Dictionary<FireObjScript, bool> _hasTaewoori = new Dictionary<FireObjScript, bool>();
    private Dictionary<FireObjScript, bool> _isRespawning = new Dictionary<FireObjScript, bool>();

    public bool IsFeverTime => _isPeverTime;
    #endregion

    #region 초기화 및 라이프사이클 메소드
    private void Awake()
    {
        // 태우리 풀 매니저 초기화
        _poolManager = FindObjectOfType<TaewooriPoolManager>();
        if (_poolManager == null)
        {
            Debug.LogWarning("[TaewooriSpawnManager] TaewooriPoolManager를 찾을 수 없습니다!");
        }

        UpdateCoolTimeSettings();
    }

    private void Start()
    {
        // 인스펙터에 설정된 화재 오브젝트 목록 초기화
        InitializeFromInspector();

        // 초기 페이즈 설정
        if (GameManager.Instance != null)
        {
            _lastPhase = GameManager.Instance.CurrentPhase;
        }

        // FireObjMgr이 초기화될 시간을 주기 위해 지연 처리
        if (_autoRegisterAllOnStart)
        {
            StartCoroutine(RegisterAllWithDelay());
        }

        _wasFeverTime = _isPeverTime;
    }

    private void Update()
    {
        HandleGamePhaseChanges();
        UpdateTaewooriesByFireState();
        CheckFeverTimeChange();
    }
    #endregion

    #region 피버타임 관리
    public void SetFeverTime(bool enabled)
    {
        // Burning 페이즈에서는 피버타임이 항상 활성화되도록 강제
        if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == GameManager.GamePhase.Burning)
        {
            _isPeverTime = true;
        }
        else
        {
            _isPeverTime = enabled;
        }
    }

    private void UpdateCoolTimeSettings()
    {
        if (_isPeverTime)
        {
            // 피버타임: 쿨타임 50% 감소
            _currentSpawnCoolTime = _spawnCoolTime * 0.5f;
            _currentRespawnCoolTime = _respawnCoolTime * 0.5f;
        }
        else
        {
            // 일반 모드: 기본 쿨타임
            _currentSpawnCoolTime = _spawnCoolTime;
            _currentRespawnCoolTime = _respawnCoolTime;
        }
    }

    private void CheckFeverTimeChange()
    {
        if (_wasFeverTime != _isPeverTime)
        {
            UpdateCoolTimeSettings();
            _wasFeverTime = _isPeverTime;
        }
    }
    #endregion

    #region 페이즈 관리
    private void HandleGamePhaseChanges()
    {
        if (GameManager.Instance == null)
            return;

        var currentPhase = GameManager.Instance.CurrentPhase;

        // 페이즈 변경 감지
        if (_lastPhase != currentPhase)
        {
            // Prevention 페이즈 이후 또는 Burning 페이즈로 진입할 때
            if (ShouldRefreshFireObjects(currentPhase))
            {
                RefreshFireObjects();
            }

            // Burning 페이즈로 진입할 때 피버타임 활성화
            if (currentPhase == GameManager.GamePhase.Burning)
            {
                SetFeverTime(true);
            }
        }

        // Burning 페이즈에서는 피버타임이 항상 활성화되도록 확인
        if (currentPhase == GameManager.GamePhase.Burning && !_isPeverTime)
        {
            SetFeverTime(true);
        }

        _lastPhase = currentPhase;
    }

    private bool ShouldRefreshFireObjects(GameManager.GamePhase currentPhase)
    {
        return (_lastPhase == GameManager.GamePhase.Prevention &&
                (currentPhase == GameManager.GamePhase.Fire || currentPhase == GameManager.GamePhase.Burning)) ||
               (_lastPhase == GameManager.GamePhase.Fire && currentPhase == GameManager.GamePhase.Burning);
    }
    #endregion

    #region 오브젝트 관리 및 등록
    private IEnumerator RegisterAllWithDelay()
    {
        yield return null; // 1프레임 기다림
        RegisterAllFireObjectsInScene();
    }

    private void InitializeFromInspector()
    {
        ClearAllFireObjectsData();

        // 인스펙터에 설정된 화재 오브젝트들 등록
        foreach (FireObjectEntry entry in _managedFireObjectsList)
        {
            if (entry.fireObject != null)
            {
                _managedFireObjects[entry.fireObject] = entry;
                _spawnTimers[entry.fireObject] = 0f;
                _hasTaewoori[entry.fireObject] = false;
                _isRespawning[entry.fireObject] = false;
            }
        }
    }

    public void RegisterAllFireObjectsInScene()
    {
        int registeredCount = 0;

        // FireObjMgr에서 모든 화재 오브젝트 가져오기
        if (FireObjMgr.Instance != null)
        {
            foreach (var zoneEntry in FireObjMgr.Instance._zoneDict)
            {
                var zone = zoneEntry.Value;
                foreach (var fireObj in zone.FireObjects)
                {
                    if (ShouldSkipFireObject(fireObj))
                        continue;

                    RegisterFireObject(fireObj);
                    registeredCount++;
                }
            }

            Debug.Log($"[TaewooriSpawnManager] 씬의 화재 오브젝트 {registeredCount}개가 등록되었습니다.");
        }
    }

    private bool ShouldSkipFireObject(FireObjScript fireObj)
    {
        // null이거나 이미 등록된 오브젝트는 건너뛰기
        if (fireObj == null || _managedFireObjects.ContainsKey(fireObj))
            return true;

        // 예방 완료된 오브젝트는 건너뛰기
        FirePreventable preventable = fireObj.GetComponent<FirePreventable>();
        return preventable != null && preventable.IsFirePreventable;
    }

    private void RegisterFireObject(FireObjScript fireObj)
    {
        // 새 항목 생성
        FireObjectEntry newEntry = new FireObjectEntry
        {
            fireObject = fireObj,
            isTaewooriOn = false // 초기에는 꺼진 상태로 시작
        };

        // 딕셔너리와 인스펙터 목록에 추가
        _managedFireObjects[fireObj] = newEntry;
        _managedFireObjectsList.Add(newEntry);

        // 타이머 및 상태 초기화
        _spawnTimers[fireObj] = 0f;
        _hasTaewoori[fireObj] = false;
        _isRespawning[fireObj] = false;
    }

    private void RefreshFireObjects()
    {
        if (FireObjMgr.Instance == null)
            return;

        FireObjMgr.Instance.RefreshAllFireObjects();
        ClearAllFireObjectsData();
        RegisterAllFireObjectsInScene();
    }

    private void ClearAllFireObjectsData()
    {
        _managedFireObjects.Clear();
        _managedFireObjectsList.Clear();
        _spawnTimers.Clear();
        _hasTaewoori.Clear();
        _isRespawning.Clear();
    }
    #endregion

    #region 태우리 관리
    private void UpdateTaewooriesByFireState()
    {
        foreach (var fireObj in new List<FireObjScript>(_managedFireObjects.Keys))
        {
            if (fireObj == null)
                continue;

            FireObjectEntry entry = _managedFireObjects[fireObj];

            // 화재 오브젝트의 IsBurning 상태를 매니저의 isTaewooriOn 상태에 반영
            if (entry.isTaewooriOn != fireObj.IsBurning && !_isAllfire)
            {
                entry.isTaewooriOn = fireObj.IsBurning;
            }

            // 매니저 상태에 따라 화재 오브젝트 업데이트
            bool shouldBeActive = _isAllfire || entry.isTaewooriOn;
            fireObj.IsBurning = shouldBeActive;

            if (shouldBeActive)
            {
                HandleActiveFire(fireObj);
            }
            else
            {
                DeactivateFire(fireObj);
            }
        }
    }

    private void HandleActiveFire(FireObjScript fireObj)
    {
        InitializeFireData(fireObj);

        if (!_hasTaewoori[fireObj])
        {
            _spawnTimers[fireObj] += Time.deltaTime;
            float requiredTime = _isRespawning[fireObj] ? _currentRespawnCoolTime : _currentSpawnCoolTime;

            if (_spawnTimers[fireObj] >= requiredTime)
            {
                SpawnTaewooriAtFireObj(fireObj);
                _spawnTimers[fireObj] = 0f;
                _isRespawning[fireObj] = false;
            }
        }
    }

    private void InitializeFireData(FireObjScript fireObj)
    {
        if (!_spawnTimers.ContainsKey(fireObj))
            _spawnTimers[fireObj] = 0f;
        if (!_hasTaewoori.ContainsKey(fireObj))
            _hasTaewoori[fireObj] = false;
        if (!_isRespawning.ContainsKey(fireObj))
            _isRespawning[fireObj] = false;
    }

    private void DeactivateFire(FireObjScript fireObj)
    {
        if (_poolManager != null)
            _poolManager.DeactivateTaewoories(fireObj);

        _spawnTimers[fireObj] = 0f;
        _hasTaewoori[fireObj] = false;
        _isRespawning[fireObj] = false;
    }

    private void SpawnTaewooriAtFireObj(FireObjScript fireObj)
    {
        if (_poolManager == null || fireObj == null)
            return;

        // 태우리가 이미 있으면 생성하지 않음
        if (_hasTaewoori.ContainsKey(fireObj) && _hasTaewoori[fireObj])
            return;

        // 예방 완료된 화재 오브젝트인지 확인
        FirePreventable preventable = fireObj.GetComponent<FirePreventable>();
        if (preventable != null && preventable.IsFirePreventable)
            return;

        // 스폰 위치 계산 (FireObjScript의 TaewooriPos 사용)
        var spawnPosition = fireObj.TaewooriPos();

        // 풀 매니저를 통해 태우리 생성
        var taewooriObj = _poolManager.SpawnTaewooriAtPosition(spawnPosition, fireObj);

        if (taewooriObj != null)
        {
            NotifyTaewooriSpawned(fireObj);
        }
    }

    public void NotifyTaewooriSpawned(FireObjScript fireObj)
    {
        if (fireObj != null && _hasTaewoori.ContainsKey(fireObj))
        {
            _hasTaewoori[fireObj] = true;
        }
    }

    public void NotifyTaewooriDestroyed(FireObjScript fireObj)
    {
        if (fireObj != null && _hasTaewoori.ContainsKey(fireObj))
        {
            _hasTaewoori[fireObj] = false;
            _isRespawning[fireObj] = true; // 리스폰 타이머 시작
            _spawnTimers[fireObj] = 0f; // 타이머 리셋
            Debug.Log($"<color=red>[TaewooriSpawnManager] {fireObj.name}의 태우리가 파괴됨, 리스폰 타이머 시작</color>");
        }
    }
    #endregion

    #region 인스펙터에서 제어하는 메소드
    // 특정 화재 오브젝트의 불 상태 설정
    public void SetFireState(FireObjScript fireObj, bool isOnFire)
    {
        if (fireObj == null || !_managedFireObjects.ContainsKey(fireObj))
            return;

        // 상태 변경
        var entry = _managedFireObjects[fireObj];
        entry.isTaewooriOn = isOnFire;
        fireObj.IsBurning = isOnFire;

        // 불이 꺼지면 관련 자원 정리
        if (!isOnFire)
        {
            DeactivateFire(fireObj);
        }
    }

    // 모든 화재 오브젝트 불 상태 설정
    public void SetAllFireState(bool isOnFire)
    {
        foreach (var entry in _managedFireObjectsList)
        {
            if (entry.fireObject != null)
            {
                entry.isTaewooriOn = isOnFire;
                entry.fireObject.IsBurning = isOnFire;

                // 불이 꺼지면 관련 자원 정리
                if (!isOnFire)
                {
                    DeactivateFire(entry.fireObject);
                }
            }
        }
    }

    // 전역 태우리 활성화/비활성화 설정
    public void SetGlobalTaewooriState(bool enabled)
    {
        _isAllfire = enabled;

        // 비활성화 시 모든 태우리 제거
        if (!enabled)
        {
            foreach (var entry in _managedFireObjectsList)
            {
                if (entry.fireObject != null)
                {
                    entry.fireObject.IsBurning = false;
                    DeactivateFire(entry.fireObject);
                }
            }
        }
    }

    // 특정 구역의 모든 화재 오브젝트 불 상태 설정
    public void SetZoneFireState(int zoneIndex, bool isOnFire)
    {
        if (FireObjMgr.Instance == null || !FireObjMgr.Instance._zoneDict.TryGetValue(zoneIndex, out MapIndex zone))
            return;

        int changedCount = 0;
        foreach (var fireObj in zone.FireObjects)
        {
            if (_managedFireObjects.ContainsKey(fireObj))
            {
                SetFireState(fireObj, isOnFire);
                changedCount++;
            }
        }
    }
    #endregion
}
