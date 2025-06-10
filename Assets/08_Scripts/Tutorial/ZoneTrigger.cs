using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    public Action onEnter;
    private int playerLayer = 9;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == playerLayer)
        {            
            onEnter?.Invoke();
        }
    }
}
