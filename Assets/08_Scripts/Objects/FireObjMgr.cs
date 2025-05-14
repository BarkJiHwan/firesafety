using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    [SerializeField] private bool _isBurningTime = false;
    public bool isPreventPhase = true;
    public bool isFirePhase = false;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        var zones = FindObjectsByType<MapIndex>(FindObjectsSortMode.None);
        foreach (var zone in zones)
        {
            if (_zoneDict.ContainsKey(zone.MapIndexValue))
            {
                continue;
            }
            _zoneDict.Add(zone.MapIndexValue, zone);
        }
    }

    void Start()
    {
        // 모든 구역 초기화
        foreach (var zone in _zoneDict.Values)
        {
            InitializeZone(zone);
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
        {
            RefreshAllFireObjects();
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
