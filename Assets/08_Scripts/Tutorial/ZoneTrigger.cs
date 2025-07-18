using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이동 튜토리얼에서 사용하는 zone 스크립트
public class ZoneTrigger : MonoBehaviour
{
    public Action onEnter;
    private int playerLayer = 9;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == playerLayer)
        {
            onEnter?.Invoke();
        }
    }
}
