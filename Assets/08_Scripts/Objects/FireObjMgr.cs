using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireObjMgr : MonoBehaviour
{
    //화재 오브젝트 리스트
    public List<FireObjScript> fireObjects = new List<FireObjScript>();
    //예방 가능한 오브젝트 리스트
    public List<FirePreventable> firePreventables = new List<FirePreventable>();

    void Start()
    {
        foreach (var fireObj in fireObjects)
        {

        }
        foreach (var firePreventable in firePreventables)
        {

        }
    }

    void Update()
    {
        
    }
}
