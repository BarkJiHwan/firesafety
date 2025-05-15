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

    [Header("태우리 생성 설정")]
    [Tooltip("태우리가 생성되는 시간 간격 (초)")]
    [Range(0f, 30f)]
    public float taewooriSpawnCoolTime = 20f;

    [Header("태우리 위치 설정")]
    [Tooltip("태우리 생성 위치에 추가할 오프셋 (x, y, z)")]
    public Vector3 taewoorSpawnOffset = Vector3.zero;

    [Tooltip("체크: TaewooriPos() + 오프셋 사용\n체크 해제: 오브젝트 위치 + 오프셋 사용")]
    public bool useBaseTaewooriPos = true;
}

public class TaewooriSpawnManager : MonoBehaviour
{
    [Header("태우리 스폰 기본 설정")]
    [Tooltip("기존 화재 오브젝트의 기본 태우리 생성 간격 (초)")]
    [SerializeField] private float defaultSpawnInterval = 5f;

    [Tooltip("태우리 생성 위치에 추가되는 기본 오프셋 (x, y, z)")]
    [SerializeField] private Vector3 defaultAdditionalOffset = Vector3.zero;

    [Header("태우리 매니저 참조")]
    [Tooltip("태우리 생성과 관리를 담당하는 TaewooriPoolManager 컴포넌트")]
    [SerializeField] private TaewooriPoolManager taewooriPoolManager;

    [Header("자동 등록 설정")]
    [Tooltip("게임 시작 시 FireObjMgr에서 모든 화재 오브젝트를 자동으로 등록")]
    [SerializeField] private bool autoRegisterAllOnStart = true;    

    [Header("화재 오브젝트 목록")]
    [Tooltip("관리할 화재 오브젝트들과 각각의 설정")]
    [SerializeField] private List<FireObjectEntry> managedFireObjectsList = new List<FireObjectEntry>();

    // 내부 상태 관리
    private Dictionary<FireObjScript, FireObjectEntry> managedFireObjects = new Dictionary<FireObjScript, FireObjectEntry>();
    private Dictionary<FireObjScript, float> spawnTimers = new Dictionary<FireObjScript, float>();
    private Dictionary<FireObjScript, bool> hasTaewoori = new Dictionary<FireObjScript, bool>(); // 태우리 존재 여부

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
    }

    private IEnumerator RegisterAllWithDelay()
    {
        // 1프레임 기다림
        yield return null;

        RegisterAllFireObjectsInScene();
    }

    private void Update()
    {
        // 모든 관리 대상 화재 오브젝트 순회
        foreach (var fireObj in new List<FireObjScript>(managedFireObjects.Keys))
        {
            if (fireObj == null)
                continue;

            FireObjectEntry entry = managedFireObjects[fireObj];
            
            if (entry.isTaewooriOn)
            {
                // 타이머 업데이트
                if (!spawnTimers.ContainsKey(fireObj))
                {
                    spawnTimers[fireObj] = 0f;
                }

                if (!hasTaewoori.ContainsKey(fireObj))
                {
                    hasTaewoori[fireObj] = false;
                }

                // 태우리가 없을 때만 타이머 증가 및 생성 처리
                if (!hasTaewoori[fireObj])
                {
                    spawnTimers[fireObj] += Time.deltaTime;

                    // 타이머가 간격을 초과했을 때 태우리 생성
                    if (spawnTimers[fireObj] >= entry.taewooriSpawnCoolTime)
                    {
                        // 태우리 생성
                        SpawnTaewooriAt(fireObj, entry.taewoorSpawnOffset, entry.useBaseTaewooriPos);

                        // 타이머 리셋
                        spawnTimers[fireObj] = 0f;
                    }
                }
            }
            else if (!entry.isTaewooriOn)
            {
                // 불이 꺼진 상태면 관련 태우리 모두 제거
                if (taewooriPoolManager != null)
                {
                    taewooriPoolManager.DeactivateTaewoories(fireObj);
                }

                // 타이머 및 상태 초기화
                spawnTimers[fireObj] = 0f;
                hasTaewoori[fireObj] = false;
            }
        }
    }

    // 인스펙터에서 설정한 화재 오브젝트 목록 초기화
    private void InitializeFromInspector()
    {
        managedFireObjects.Clear();
        hasTaewoori.Clear();

        // 인스펙터에 설정된 화재 오브젝트들 등록
        foreach (FireObjectEntry entry in managedFireObjectsList)
        {
            if (entry.fireObject != null)
            {
                managedFireObjects[entry.fireObject] = entry;
                spawnTimers[entry.fireObject] = 0f;
                hasTaewoori[entry.fireObject] = false;

                // 기본값 설정
                if (entry.taewooriSpawnCoolTime <= 0)
                {
                    entry.taewooriSpawnCoolTime = defaultSpawnInterval;
                }

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
            FireObjectEntry entry = managedFireObjects[fireObj];
            entry.isTaewooriOn = isOnFire;

            // 불이 꺼지면 관련 자원 정리
            if (!isOnFire)
            {
                if (taewooriPoolManager != null)
                {
                    taewooriPoolManager.DeactivateTaewoories(fireObj);
                }

                spawnTimers[fireObj] = 0f;
                hasTaewoori[fireObj] = false;
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

                // 불이 꺼지면 관련 자원 정리
                if (!isOnFire)
                {
                    if (taewooriPoolManager != null)
                    {
                        taewooriPoolManager.DeactivateTaewoories(entry.fireObject);
                    }

                    spawnTimers[entry.fireObject] = 0f;
                    hasTaewoori[entry.fireObject] = false;
                }
            }
        }

        Debug.Log($"[TaewooriSpawnManager] 모든 화재 오브젝트 불 상태 변경: {isOnFire}");
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
            Debug.Log($"[TaewooriSpawnManager] {fireObj.name}의 태우리가 파괴됨");
        }
    }

    // 화재 오브젝트에 태우리 생성 (TaewooriPos 사용 + 추가 오프셋)
    private void SpawnTaewooriAt(FireObjScript fireObj, Vector3 additionalOffset, bool useBaseTaewooriPos)
    {
        if (taewooriPoolManager != null && fireObj != null)
        {
            // 태우리가 이미 있으면 생성하지 않음
            if (hasTaewoori.ContainsKey(fireObj) && hasTaewoori[fireObj])
            {
                return;
            }

            // 스폰 위치 계산
            Vector3 spawnPosition;

            if (useBaseTaewooriPos)
            {
                // TaewooriPos()를 기반으로 추가 오프셋 적용
                spawnPosition = fireObj.TaewooriPos() + additionalOffset;
            }
            else
            {
                // 오브젝트 위치를 기반으로 오프셋만 사용
                spawnPosition = fireObj.transform.position + additionalOffset;
            }

            // 풀 매니저를 통해 태우리 생성
            GameObject taewooriObj = taewooriPoolManager.SpawnTaewooriAtPosition(spawnPosition, fireObj);

            if (taewooriObj != null)
            {
                NotifyTaewooriSpawned(fireObj);
                Debug.Log($"[TaewooriSpawnManager] {fireObj.name}에 태우리 생성됨, 위치: {spawnPosition}");
            }
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
                MapIndex zone = zoneEntry.Value;
                foreach (var fireObj in zone.FireObjects)
                {
                    // 이미 인스펙터에서 등록된 오브젝트는 건너뛰기
                    if (managedFireObjects.ContainsKey(fireObj) || fireObj == null)
                    {
                        continue;
                    }

                    // 새 항목 생성
                    FireObjectEntry newEntry = new FireObjectEntry
                    {
                        fireObject = fireObj,
                        isTaewooriOn = false,
                        taewooriSpawnCoolTime = defaultSpawnInterval,
                        taewoorSpawnOffset = defaultAdditionalOffset,
                        useBaseTaewooriPos = true
                    };

                    // 딕셔너리와 인스펙터 목록에 추가
                    managedFireObjects[fireObj] = newEntry;
                    managedFireObjectsList.Add(newEntry);

                    // 타이머 및 상태 초기화
                    spawnTimers[fireObj] = 0f;
                    hasTaewoori[fireObj] = false;

                    registeredCount++;
                }
            }

            Debug.Log($"[TaewooriSpawnManager] 씬의 화재 오브젝트 {registeredCount}개가 등록되었습니다.");
        }
        else
        {
            Debug.LogWarning("[TaewooriSpawnManager] FireObjMgr.Instance가 null입니다!");
        }
    }

    // 태우리 생성 위치를 시각적으로 표시 (디버그용)
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        foreach (var entry in managedFireObjectsList)
        {
            if (entry.fireObject != null && entry.isTaewooriOn)
            {
                Vector3 spawnPos;

                if (entry.useBaseTaewooriPos)
                {
                    spawnPos = entry.fireObject.TaewooriPos() + entry.taewoorSpawnOffset;
                }
                else
                {
                    spawnPos = entry.fireObject.transform.position + entry.taewoorSpawnOffset;
                }

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(spawnPos, 0.1f);

                // 라인도 그려서 오브젝트와 스폰 위치 연결
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(entry.fireObject.transform.position, spawnPos);
            }
        }
    }
}
