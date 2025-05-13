using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireObjMgr : MonoBehaviour
{
    private static FireObjMgr _instance;
    public static FireObjMgr Instance
    {
        get
        {
            if(_instance == null)
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

    //화재 오브젝트 리스트
    public List<FireObjScript> fireObjects = new List<FireObjScript>();
    //예방 가능한 오브젝트 리스트
    public List<FirePreventable> firePreventables = new List<FirePreventable>();

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        //예방 페이즈라면 1회 시작 > 버닝 페이즈에서 IsBurning을 true로 바꿔주면 됨
        foreach (var fireObj in fireObjects)
        {
            fireObj.IsBurning = false;
        }
        //firePreventables(예방 가능한 오브젝트)리스트는 최초 시작에서 한 번만
        foreach (var firePreventable in firePreventables)
        {
            firePreventable.IsFirePreventable = false;
        }
    }
    void Update()
    {

    }
}
