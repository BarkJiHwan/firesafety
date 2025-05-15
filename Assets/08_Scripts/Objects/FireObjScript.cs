using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireObjScript : MonoBehaviour, ITaewooriPos
{
    [Header("true일 때 태우리 생성 가능 상태"), Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isBurning;
    [SerializeField] private float _fireElementalSummonTime = 5f; //태우리 생성 타이머

    [Range(0.01f, 0.5f), Header("윗면 기준으로 스폰할 오프셋")]
    [SerializeField] private float _spawnOffset = 0.1f;

    public bool IsBurning
    {
        get => _isBurning;
        set => _isBurning = value;
    }    

    private void Start()
    {
        _isBurning = false;
    }

    // 스폰 위치 계산 (윗면)
    public Vector3 TaewooriPos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"{gameObject.name}에 Collider가 없습니다!");
            return transform.position + Vector3.up * _spawnOffset;
        }

        Vector3 colTopPos = col.bounds.center + Vector3.up * (col.bounds.extents.y + _spawnOffset);
        return colTopPos;
    }

    void Update()
    {
        //잠깐 주석 쳐놓음 
        //if (_isBurning)
        //{
        //    _isBurning = false;
        //}
    }

    // CHM 
    // 태우리 관련 메서드들은 TaewooriSpawnManager와 호환성을 위해 유지
    public void NotifyTaewooriSpawned(Taewoori taewoori)
    {
        // TaewooriSpawnManager에서 호출됨
    }

    public void NotifyTaewooriDestroyed(Taewoori taewoori)
    {
        // TaewooriSpawnManager에서 호출됨
    }

    public void NotifyAllTaewooriDestroyed()
    {
        // TaewooriSpawnManager에서 호출됨
    }
}
