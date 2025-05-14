using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireObjScript : MonoBehaviour, ITaewooriPos
{
    [Header("true일 때 태우리 생성 가능 상태"),Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isBurning;
    [SerializeField] private float _fireElementalSummonTime; //태우리 생성 타이머

    [Range(0.01f, 0.5f), Header("윗면 기준으로 스폰할 오프셋")]
    [SerializeField] private float _spawnOffset = 0.1f;
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
    // 스폰 위치 계산 (윗면)
    public Vector3 TaewooriPos()
    {
        Collider col = GetComponent<Collider>();
        Vector3 colTopPos = col.bounds.center + Vector3.up * (col.bounds.extents.y + _spawnOffset);
        return colTopPos;
    }
    void Update()
    {
        //생성 테스트
        if (_isBurning)
        {
            _isBurning = false;
        }
    }
}
