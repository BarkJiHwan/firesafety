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
    [Header("전역 태우리 설정")]
    [Tooltip("모든 화재 오브젝트의 태우리 활성화 여부를 한 번에 제어")]
    [SerializeField] private bool _isAllfire = false;

    [Header("태우리 생성 설정")]
    [Tooltip("태우리가 처음 생성되는 시간 간격 (초)")]
    [SerializeField] private float taewooriSpawnCoolTime = 10f;

    [Tooltip("태우리가 파괴된 후 재생성되는 시간 간격 (초)")]
    [SerializeField] private float taewooriRespawnCoolTime = 10f;

    [Header("피버타임 설정")]
    [Tooltip("체크하면 피버타임 모드가 활성화됩니다.")]
    [SerializeField] private bool _isPeverTime = false;

    private float currentSpawnCoolTime;
    private float currentRespawnCoolTime;

    private bool wasFeverTime = false;

    public bool IsFeverTime => _isPeverTime;

    private TaewooriPoolManager taewooriPoolManager;
    private bool autoRegisterAllOnStart = true;

    [Header("화재 오브젝트 목록")]
    [Tooltip("관리할 화재 오브젝트들과 각각의 설정")]
    [SerializeField] private List<FireObjectEntry> managedFireObjectsList = new List<FireObjectEntry>();

    // 내부 상태 관리
    private Dictionary<FireObjScript, FireObjectEntry> managedFireObjects = new Dictionary<FireObjScript, FireObjectEntry>();
    private Dictionary<FireObjScript, float> spawnTimers = new Dictionary<FireObjScript, float>();
    private Dictionary<FireObjScript, bool> hasTaewoori = new Dictionary<FireObjScript, bool>();
    private Dictionary<FireObjScript, bool> isRespawning = new Dictionary<FireObjScript, bool>();

    private void Awake()
    {
        // 태우리 풀 매니저가 할당되지 않았다면 찾기
        if (taewooriPoolManager == null)
        {
            taewooriPoolManager = FindObjectOfType<TaewooriPoolManager>();
            if (taewooriPoolManager == null)
            {
                Debug.LogWarning("[TaewooriSpawnManager] TaewooriPoolManager를 찾을 수 없습니다!");
            }
        }
        UpdateCoolTimeSettings();
    }

    private void Start()
    {
        // 인스펙터에 설정된 화재 오브젝트 목록 초기화
        InitializeFromInspector();

        // 약간 지연시켜 FireObjMgr이 초기화될 시간을 줌
        if (autoRegisterAllOnStart)
        {
            StartCoroutine(RegisterAllWithDelay());
        }
        wasFeverTime = _isPeverTime;
    }

    private IEnumerator RegisterAllWithDelay()
    {
        // 1프레임 기다림
        yield return null;

        RegisterAllFireObjectsInScene();
    }
    public void SetFeverTime(bool enabled)
    {
        _isPeverTime = enabled;
    }

    private void Update()
    {
        foreach (var fireObj in new List<FireObjScript>(managedFireObjects.Keys))
        {
            if (fireObj == null)
                continue;

            FireObjectEntry entry = managedFireObjects[fireObj];
            bool shouldBeActive = _isAllfire || entry.isTaewooriOn;
            fireObj.IsBurning = shouldBeActive;

            if (shouldBeActive)
            {
                if (!spawnTimers.ContainsKey(fireObj))
                    spawnTimers[fireObj] = 0f;
                if (!hasTaewoori.ContainsKey(fireObj))
                    hasTaewoori[fireObj] = false;
                if (!isRespawning.ContainsKey(fireObj))
                    isRespawning[fireObj] = false;

                if (!hasTaewoori[fireObj])
                {
                    spawnTimers[fireObj] += Time.deltaTime;
                    float requiredTime = isRespawning[fireObj] ? currentRespawnCoolTime : currentSpawnCoolTime;

                    if (spawnTimers[fireObj] >= requiredTime)
                    {
                        SpawnTaewooriAtFireObj(fireObj);
                        spawnTimers[fireObj] = 0f;
                        isRespawning[fireObj] = false;
                    }
                }
            }
            else
            {
                if (taewooriPoolManager != null)
                    taewooriPoolManager.DeactivateTaewoories(fireObj);

                spawnTimers[fireObj] = 0f;
                hasTaewoori[fireObj] = false;
                isRespawning[fireObj] = false;
            }
        }
        if (wasFeverTime != _isPeverTime)
        {
            // 쿨타임 설정 업데이트
            UpdateCoolTimeSettings();

            wasFeverTime = _isPeverTime;
        }
    }

    // 인스펙터에서 설정한 화재 오브젝트 목록 초기화
    private void InitializeFromInspector()
    {
        managedFireObjects.Clear();
        hasTaewoori.Clear();
        isRespawning.Clear();

        // 인스펙터에 설정된 화재 오브젝트들 등록
        foreach (FireObjectEntry entry in managedFireObjectsList)
        {
            if (entry.fireObject != null)
            {
                managedFireObjects[entry.fireObject] = entry;
                spawnTimers[entry.fireObject] = 0f;
                hasTaewoori[entry.fireObject] = false;
                isRespawning[entry.fireObject] = false;

                Debug.Log($"[TaewooriSpawnManager] 인스펙터에서 {entry.fireObject.name} 등록됨, 불 상태: {entry.isTaewooriOn}");
            }
        }
    }

    // 특정 화재 오브젝트의 불 상태 설정
    public void SetFireState(FireObjScript fireObj, bool isOnFire)
    {
        if (fireObj != null && managedFireObjects.ContainsKey(fireObj))
        {
            // 상태 변경
            var entry = managedFireObjects[fireObj];
            entry.isTaewooriOn = isOnFire;
            fireObj.IsBurning = isOnFire;

            // 불이 꺼지면 관련 자원 정리
            if (!isOnFire)
            {
                if (taewooriPoolManager != null)
                {
                    taewooriPoolManager.DeactivateTaewoories(fireObj);
                }

                spawnTimers[fireObj] = 0f;
                hasTaewoori[fireObj] = false;
                isRespawning[fireObj] = false;
            }

            Debug.Log($"[TaewooriSpawnManager] {fireObj.name}의 불 상태 변경: {isOnFire}");
        }
    }

    // 모든 화재 오브젝트 불 상태 설정
    public void SetAllFireState(bool isOnFire)
    {
        foreach (var entry in managedFireObjectsList)
        {
            if (entry.fireObject != null)
            {
                entry.isTaewooriOn = isOnFire;
                entry.fireObject.IsBurning = isOnFire;

                // 불이 꺼지면 관련 자원 정리
                if (!isOnFire)
                {
                    if (taewooriPoolManager != null)
                    {
                        taewooriPoolManager.DeactivateTaewoories(entry.fireObject);
                    }

                    spawnTimers[entry.fireObject] = 0f;
                    hasTaewoori[entry.fireObject] = false;
                    isRespawning[entry.fireObject] = false;
                }
            }
        }

        Debug.Log($"[TaewooriSpawnManager] 모든 화재 오브젝트 불 상태 변경: {isOnFire}");
    }

    // 전역 태우리 활성화/비활성화 설정
    public void SetGlobalTaewooriState(bool enabled)
    {
        _isAllfire = enabled;

        // 비활성화 시 모든 태우리 제거
        if (!enabled)
        {
            foreach (var entry in managedFireObjectsList)
            {
                if (entry.fireObject != null)
                {
                    entry.fireObject.IsBurning = false;

                    if (taewooriPoolManager != null)
                    {
                        taewooriPoolManager.DeactivateTaewoories(entry.fireObject);
                    }

                    spawnTimers[entry.fireObject] = 0f;
                    hasTaewoori[entry.fireObject] = false;
                    isRespawning[entry.fireObject] = false;
                }
            }
        }

        Debug.Log($"[TaewooriSpawnManager] 전역 태우리 상태 변경: {enabled}");
    }

    // 특정 구역의 모든 화재 오브젝트 불 상태 설정
    public void SetZoneFireState(int zoneIndex, bool isOnFire)
    {
        if (FireObjMgr.Instance != null && FireObjMgr.Instance._zoneDict.TryGetValue(zoneIndex, out MapIndex zone))
        {
            int changedCount = 0;
            foreach (var fireObj in zone.FireObjects)
            {
                if (managedFireObjects.ContainsKey(fireObj))
                {
                    SetFireState(fireObj, isOnFire);
                    changedCount++;
                }
            }

            Debug.Log($"[TaewooriSpawnManager] 구역 {zoneIndex}의 화재 오브젝트 {changedCount}개의 불 상태 변경: {isOnFire}");
        }
    }

    // 태우리가 생성되었을 때 호출
    public void NotifyTaewooriSpawned(FireObjScript fireObj)
    {
        if (fireObj != null && hasTaewoori.ContainsKey(fireObj))
        {
            hasTaewoori[fireObj] = true;
            Debug.Log($"[TaewooriSpawnManager] {fireObj.name}에 태우리 생성됨");
        }
    }

    // 태우리가 파괴되었을 때 호출
    public void NotifyTaewooriDestroyed(FireObjScript fireObj)
    {
        if (fireObj != null && hasTaewoori.ContainsKey(fireObj))
        {
            hasTaewoori[fireObj] = false;
            isRespawning[fireObj] = true; // 리스폰 타이머 시작
            spawnTimers[fireObj] = 0f; // 타이머 리셋
            Debug.Log($"<color=red> [TaewooriSpawnManager] {fireObj.name}의 태우리가 파괴됨, 리스폰 타이머 시작");
        }
    }

    // 화재 오브젝트에 태우리 생성
    private void SpawnTaewooriAtFireObj(FireObjScript fireObj)
    {
        if (taewooriPoolManager == null || fireObj == null)
        {
            Debug.LogError($"[TaewooriSpawnManager] SpawnTaewooriAt 실패 - taewooriPoolManager: {taewooriPoolManager}, fireObj: {fireObj}");
            return;
        }

        // 태우리가 이미 있으면 생성하지 않음
        if (hasTaewoori.ContainsKey(fireObj) && hasTaewoori[fireObj])
        {
            return;
        }

        // 스폰 위치 계산 (FireObjScript의 TaewooriPos 사용)
        var spawnPosition = fireObj.TaewooriPos();

        // 풀 매니저를 통해 태우리 생성
        var taewooriObj = taewooriPoolManager.SpawnTaewooriAtPosition(spawnPosition, fireObj);

        if (taewooriObj != null)
        {
            NotifyTaewooriSpawned(fireObj);
        }

    }

    // 게임 시작 시 모든 구역의 화재 오브젝트 자동 등록
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
                    // 이미 인스펙터에서 등록된 오브젝트는 건너뛰기
                    if (managedFireObjects.ContainsKey(fireObj) || fireObj == null)
                    {
                        continue;
                    }

                    // 새 항목 생성
                    FireObjectEntry newEntry = new()
                    {
                        fireObject = fireObj,
                        isTaewooriOn = false // 초기에는 꺼진 상태로 시작
                    };

                    // 딕셔너리와 인스펙터 목록에 추가
                    managedFireObjects[fireObj] = newEntry;
                    managedFireObjectsList.Add(newEntry);

                    // 타이머 및 상태 초기화
                    spawnTimers[fireObj] = 0f;
                    hasTaewoori[fireObj] = false;
                    isRespawning[fireObj] = false;

                    registeredCount++;
                }
            }

            Debug.Log($"[TaewooriSpawnManager] 씬의 화재 오브젝트 {registeredCount}개가 등록되었습니다.");
        }
        else
        {
            Debug.LogWarning("FireObjMgr.Instance    null");
        }
    }
    private void UpdateCoolTimeSettings()
    {
        if (_isPeverTime)
        {
            // 피버타임: 쿨타임 50% 감소
            currentSpawnCoolTime = taewooriSpawnCoolTime * 0.5f;
            currentRespawnCoolTime = taewooriRespawnCoolTime * 0.5f;
            Debug.Log($"쿨타임 50% 감소 ");
        }
        else
        {
            // 일반 모드: 기본 쿨타임
            currentSpawnCoolTime = taewooriSpawnCoolTime;
            currentRespawnCoolTime = taewooriRespawnCoolTime;
            Debug.Log($"일반 쿨타임");
        }
    }
}
