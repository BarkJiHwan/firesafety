using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireObjScript : MonoBehaviour
{
    [Header("true일 때 태우리 생성 가능 상태"),Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isBurning;
    [SerializeField] private float _fireElementalSummonTime; //태우리 생성 타이머
    public bool IsBurning
    {
        get => _isBurning;
        set => _isBurning = value;
    }
    public float FireElementalSummonTime
    {
        get => _fireElementalSummonTime;
        set => _fireElementalSummonTime = value;
    }
    private void Start()
    {
        _isBurning = false;
    }
}
