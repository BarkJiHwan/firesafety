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
    [Tooltip("태우리가 처음 생성되는 시간 간격 (초)")]
    [Range(0f, 30f)]
    public float taewooriSpawnCoolTime = 20f;

    [Tooltip("태우리가 파괴된 후 재생성되는 시간 간격 (초)")]
    [Range(0f, 30f)]
    public float taewooriRespawnCoolTime = 10f;

    [Header("태우리 오프셋 설정")]
    [Tooltip("태우리 생성 위치에 변경할 오프셋 (x, y, z)")]
    public Vector3 defaultAdditionalOffset = Vector3.zero;
}

public class TaewooriSpawnManager : MonoBehaviour
{
    [Header("전역 태우리 설정")]
    [Tooltip("모든 화재 오브젝트의 태우리 활성화 여부를 한 번에 제어")]
    [SerializeField] private bool globalTaewooriEnabled = false;

    [Header("태우리 오프셋 저장 설정")]
    [Tooltip("체크하면 태우리 생성 위치 오프셋 설정을 세션 간에 유지합니다")]
    [SerializeField] private bool saveTaewooriOffsetsBetweenSessions = true;

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
    private Dictionary<FireObjScript, bool> isRespawning = new Dictionary<FireObjScript, bool>(); // 리스폰 중인지 여부

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

        // 저장된 태우리 오프셋 값 불러오기
        if (saveTaewooriOffsetsBetweenSessions)
        {
            LoadTaewooriOffsets();
        }

        // 약간 지연시켜 FireObjMgr이 초기화될 시간을 줌
        if (autoRegisterAllOnStart)
        {
            StartCoroutine(RegisterAllWithDelay());
        }
    }

    private void OnApplicationQuit()
    {
        // 태우리 오프셋 값 저장
        if (saveTaewooriOffsetsBetweenSessions)
        {
            SaveTaewooriOffsets();
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

            // 전역 설정 또는 개별 설정으로 태우리 활성화
            bool shouldBeActive = globalTaewooriEnabled || entry.isTaewooriOn;

            // 화재 오브젝트의 실제 불 상태 반영 (실제 IsBurning 상태 사용)
            fireObj.IsBurning = shouldBeActive;

            if (shouldBeActive)
            {
                // 타이머 초기화
                if (!spawnTimers.ContainsKey(fireObj))
                {
                    spawnTimers[fireObj] = 0f;
                }

                if (!hasTaewoori.ContainsKey(fireObj))
                {
                    hasTaewoori[fireObj] = false;
                }

                if (!isRespawning.ContainsKey(fireObj))
                {
                    isRespawning[fireObj] = false;
                }

                // 태우리가 없을 때만 타이머 증가 및 생성 처리
                if (!hasTaewoori[fireObj])
                {
                    spawnTimers[fireObj] += Time.deltaTime;

                    // 타이머가 간격을 초과했을 때 태우리 생성
                    float requiredTime = isRespawning[fireObj] ? entry.taewooriRespawnCoolTime : entry.taewooriSpawnCoolTime;

                    if (spawnTimers[fireObj] >= requiredTime)
                    {
                        // 태우리 생성 (FireObjScript의 설정 사용)
                        SpawnTaewooriAtFireObj(fireObj);

                        // 타이머 리셋 및 리스폰 상태 리셋
                        spawnTimers[fireObj] = 0f;
                        isRespawning[fireObj] = false;
                    }
                }
            }
            else if (!shouldBeActive)
            {
                // 불이 꺼진 상태면 관련 태우리 모두 제거
                if (taewooriPoolManager != null)
                {
                    taewooriPoolManager.DeactivateTaewoories(fireObj);
                }

                // 타이머 및 상태 초기화
                spawnTimers[fireObj] = 0f;
                hasTaewoori[fireObj] = false;
                isRespawning[fireObj] = false;
            }
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

                // FireObjectEntry의 오프셋 값을 FireObjScript에 적용
                if (entry.defaultAdditionalOffset != Vector3.zero)
                {
                    entry.fireObject.SpawnOffset = entry.defaultAdditionalOffset;
                }

                Debug.Log($"[TaewooriSpawnManager] 인스펙터에서 {entry.fireObject.name} 등록됨, 불 상태: {entry.isTaewooriOn}, 오프셋: {entry.fireObject.SpawnOffset}");
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
        globalTaewooriEnabled = enabled;

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
            Debug.Log($"[TaewooriSpawnManager] {fireObj.name}의 태우리가 파괴됨, 리스폰 타이머 시작");
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
        Vector3 spawnPosition = fireObj.TaewooriPos();

        // 풀 매니저를 통해 태우리 생성
        GameObject taewooriObj = taewooriPoolManager.SpawnTaewooriAtPosition(spawnPosition, fireObj);

        if (taewooriObj != null)
        {
            NotifyTaewooriSpawned(fireObj);
            Debug.Log($"[TaewooriSpawnManager] {fireObj.name}에 태우리 생성됨, 위치: {spawnPosition}");
        }
        else
        {
            Debug.LogError($"[TaewooriSpawnManager] {fireObj.name}에 태우리 생성 실패");
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
                        isTaewooriOn = false, // 초기에는 꺼진 상태로 시작
                        taewooriSpawnCoolTime = 20f, // 기본값 직접 설정
                        taewooriRespawnCoolTime = 10f, // 기본값 직접 설정
                        defaultAdditionalOffset = Vector3.zero // 기본 오프셋은 없음
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
            Debug.LogWarning("[TaewooriSpawnManager] FireObjMgr.Instance가 null입니다!");
        }
    }

    // 태우리 오프셋 저장 메서드
    private void SaveTaewooriOffsets()
    {
        foreach (var entry in managedFireObjectsList)
        {
            if (entry.fireObject != null)
            {
                string id = entry.fireObject.GetInstanceID().ToString();

                // FireObjScript의 태우리 오프셋 값 가져와서 FireObjectEntry에 저장
                Vector3 offset = entry.fireObject.SpawnOffset;
                entry.defaultAdditionalOffset = offset;

                // PlayerPrefs에도 저장
                PlayerPrefs.SetFloat("TaewooriOffsetX_" + id, offset.x);
                PlayerPrefs.SetFloat("TaewooriOffsetY_" + id, offset.y);
                PlayerPrefs.SetFloat("TaewooriOffsetZ_" + id, offset.z);

                // UseColliderCenter 설정 저장
                PlayerPrefs.SetInt("TaewooriUseCollider_" + id, entry.fireObject.UseColliderCenter ? 1 : 0);
            }
        }

        PlayerPrefs.Save();
        Debug.Log("[TaewooriSpawnManager] 모든 태우리 생성 위치 오프셋 설정이 저장되었습니다.");
    }

    // 태우리 오프셋 불러오기 메서드
    private void LoadTaewooriOffsets()
    {
        foreach (var entry in managedFireObjectsList)
        {
            if (entry.fireObject != null)
            {
                string id = entry.fireObject.GetInstanceID().ToString();

                // 저장된 오프셋 값 확인
                if (PlayerPrefs.HasKey("TaewooriOffsetX_" + id))
                {
                    float x = PlayerPrefs.GetFloat("TaewooriOffsetX_" + id);
                    float y = PlayerPrefs.GetFloat("TaewooriOffsetY_" + id);
                    float z = PlayerPrefs.GetFloat("TaewooriOffsetZ_" + id);
                    Vector3 loadedOffset = new Vector3(x, y, z);

                    // FireObjScript와 FireObjectEntry 모두에 오프셋 적용
                    entry.fireObject.SpawnOffset = loadedOffset;
                    entry.defaultAdditionalOffset = loadedOffset;

                    // UseColliderCenter 설정 불러오기
                    if (PlayerPrefs.HasKey("TaewooriUseCollider_" + id))
                    {
                        entry.fireObject.UseColliderCenter = PlayerPrefs.GetInt("TaewooriUseCollider_" + id) == 1;
                    }
                }
                else
                {
                    // 저장된 설정이 없으면 FireObjectEntry의 설정 사용
                    entry.fireObject.SpawnOffset = entry.defaultAdditionalOffset;
                }
            }
        }

        Debug.Log("[TaewooriSpawnManager] 모든 태우리 생성 위치 오프셋 설정이 불러와졌습니다.");
    }

    // 모든 태우리 오프셋 설정 초기화 메서드
    public void ResetAllTaewooriOffsets()
    {
        foreach (var entry in managedFireObjectsList)
        {
            if (entry.fireObject != null)
            {
                // 오프셋 초기화
                entry.fireObject.SpawnOffset = Vector3.zero;
                entry.defaultAdditionalOffset = Vector3.zero;
                entry.fireObject.UseColliderCenter = true;

                // 저장된 설정 삭제
                string id = entry.fireObject.GetInstanceID().ToString();
                PlayerPrefs.DeleteKey("TaewooriOffsetX_" + id);
                PlayerPrefs.DeleteKey("TaewooriOffsetY_" + id);
                PlayerPrefs.DeleteKey("TaewooriOffsetZ_" + id);
                PlayerPrefs.DeleteKey("TaewooriUseCollider_" + id);
            }
        }

        PlayerPrefs.Save();
        Debug.Log("[TaewooriSpawnManager] 모든 태우리 생성 위치 오프셋 설정이 초기화되었습니다.");
    }

    // 특정 화재 오브젝트의 태우리 오프셋 설정
    public void SetTaewooriOffset(FireObjScript fireObj, Vector3 offset, bool useColliderCenter)
    {
        if (fireObj != null)
        {
            fireObj.SpawnOffset = offset;
            fireObj.UseColliderCenter = useColliderCenter;

            // FireObjectEntry에도 오프셋 설정 업데이트
            if (managedFireObjects.TryGetValue(fireObj, out FireObjectEntry entry))
            {
                entry.defaultAdditionalOffset = offset;
            }

            // 변경 즉시 저장
            if (saveTaewooriOffsetsBetweenSessions)
            {
                string id = fireObj.GetInstanceID().ToString();
                PlayerPrefs.SetFloat("TaewooriOffsetX_" + id, offset.x);
                PlayerPrefs.SetFloat("TaewooriOffsetY_" + id, offset.y);
                PlayerPrefs.SetFloat("TaewooriOffsetZ_" + id, offset.z);
                PlayerPrefs.SetInt("TaewooriUseCollider_" + id, useColliderCenter ? 1 : 0);
                PlayerPrefs.Save();
            }

            Debug.Log($"[TaewooriSpawnManager] {fireObj.name}의 태우리 오프셋이 {offset}로 설정되었습니다. 콜라이더 중심 사용: {useColliderCenter}");
        }
    }

    // 태우리 생성 위치를 시각적으로 표시 (디버그용)
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        foreach (var entry in managedFireObjectsList)
        {
            if (entry.fireObject != null && (entry.isTaewooriOn || globalTaewooriEnabled))
            {
                // 스폰 위치 계산 (FireObjScript의 TaewooriPos 사용)
                Vector3 spawnPos = entry.fireObject.TaewooriPos();

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(spawnPos, 0.1f);

                // 라인도 그려서 오브젝트와 스폰 위치 연결
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(entry.fireObject.transform.position, spawnPos);
            }
        }
    }
    // TaewooriSpawnManager에 추가할 메서드
    public void SetAllUseColliderCenter(bool useColliderCenter)
    {
        foreach (var entry in managedFireObjectsList)
        {
            if (entry.fireObject != null)
            {
                // 모든 화재 오브젝트의 UseColliderCenter 값 설정
                entry.fireObject.UseColliderCenter = useColliderCenter;

                // 변경된 설정 즉시 저장 (필요시)
                if (saveTaewooriOffsetsBetweenSessions)
                {
                    string id = entry.fireObject.GetInstanceID().ToString();
                    PlayerPrefs.SetInt("TaewooriUseCollider_" + id, useColliderCenter ? 1 : 0);
                }
            }
        }

        // 변경 사항 저장
        if (saveTaewooriOffsetsBetweenSessions)
        {
            PlayerPrefs.Save();
        }

        Debug.Log($"[TaewooriSpawnManager] 모든 화재 오브젝트의 UseColliderCenter 설정: {useColliderCenter}");
    }
}
