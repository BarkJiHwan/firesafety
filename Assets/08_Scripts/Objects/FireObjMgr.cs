using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [SerializeField] private bool isPreventPhase = false;
    [SerializeField] private bool isFirePhase = false;

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
        if (isPreventPhase)
        {
            Debug.Log("모든 구역 초기화 완료");
            // 모든 구역 초기화
            foreach (var zone in _zoneDict.Values)
            {
                InitializeZone(zone);
            }
            isPreventPhase = false;
        }
        if (isFirePhase)
        {
            Debug.Log("화재 페이즈 진입 완료");
            RefreshAllFireObjects();
            isFirePhase = false;
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
        foreach (var fireObj in zone.FireObjects)
        {
            fireObj.IsBurning = false;
        }
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
}
