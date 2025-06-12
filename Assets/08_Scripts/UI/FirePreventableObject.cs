using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirePreventableObject
{
    public GameObject preventObject { get; private set; }
    public PreventType preventType { get; private set; }
    public bool isHaveChild { get; private set; }
    public GameObject childObject { get; private set; }
    

    public FirePreventableObject(GameObject gameObject, PreventType type, bool isChild)
    {
        preventObject = gameObject;
        preventType = type;
        isHaveChild = isChild;
        if(isHaveChild == true)
        {
            childObject = gameObject.transform.GetChild(0).gameObject;
        }
    }
}
