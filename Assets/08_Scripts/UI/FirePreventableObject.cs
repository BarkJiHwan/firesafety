using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirePreventableObject
{
    // 예방 오브젝트
    public GameObject preventObject { get; private set; }
    // 예방 오브젝트 타입
    public PreventType preventType { get; private set; }
    // 예방 오브젝트 자식 여부
    public bool isHaveChild { get; private set; }
    // 예방 오브젝트 자식 오브젝트
    public GameObject childObject { get; private set; }
    
    public FirePreventableObject(GameObject gameObject, PreventType type, bool isChild)
    {
        preventObject = gameObject;
        preventType = type;
        isHaveChild = isChild;
        // 해당 오브젝트에 자식이 있으면
        if(isHaveChild == true)
        {
            // 자식 오브젝트 등록
            childObject = gameObject.transform.GetChild(0).gameObject;
        }
    }
}
