using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireObjScript : MonoBehaviour, ITaewooriPos
{
    //    [Header("true일 때 태우리 생성 가능 상태"), Tooltip("체크가 되어 있으면 트루입니다.")]
    //    [SerializeField] private bool _isBurning;    

    //    [Range(0.01f, 0.5f), Header("윗면 기준으로 스폰할 오프셋")]
    //    [SerializeField] private float _spawnOffset = 0.1f;

    //    public bool IsBurning
    //    {
    //        get => _isBurning;
    //        set => _isBurning = value;
    //    }    

    //    private void Start()
    //    {
    //        _isBurning = false;
    //    }

    //    // 스폰 위치 계산 (윗면)

    //    public Vector3 TaewooriPos()
    //    {
    //        Collider col = GetComponent<Collider>();
    //        if (col == null)
    //        {
    //            Debug.LogWarning($"{gameObject.name}에 Collider가 없습니다!");
    //            return transform.position + Vector3.up * _spawnOffset;
    //        }

    //        Vector3 colTopPos = col.bounds.center + Vector3.up * (col.bounds.extents.y + _spawnOffset);
    //        return colTopPos;
    //    }

    //    void Update()
    //    {
    //        //잠깐 주석 쳐놓음 
    //        //if (_isBurning)
    //        //{
    //        //    _isBurning = false;
    //        //}
    //    }

    //    // CHM 
    //    // 태우리 관련 메서드들은 TaewooriSpawnManager와 호환성을 위해 유지
    //    public void NotifyTaewooriSpawned(Taewoori taewoori)
    //    {
    //        // TaewooriSpawnManager에서 호출됨
    //    }

    //    public void NotifyTaewooriDestroyed(Taewoori taewoori)
    //    {
    //        // TaewooriSpawnManager에서 호출됨
    //    }

    //    public void NotifyAllTaewooriDestroyed()
    //    {
    //        // TaewooriSpawnManager에서 호출됨
    //    }
    //}

    //태우리 스폰 매니저에서 변경한 오프셋값 저장하기위해 변경함 

    [Header("true일 때 태우리 생성 가능 상태"), Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isBurning;

    [Header("태우리 스폰 위치 설정")]
    [Tooltip("태우리 생성 위치에 추가할 오프셋 (x, y, z)")]
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.1f, 0f);

    [Tooltip("체크: 콜라이더 중심 + 오프셋 사용\n체크 해제: 오브젝트 위치 + 오프셋 사용")]
    [SerializeField] private bool _useColliderCenter = true;

    public bool IsBurning
    {
        get => _isBurning;
        set => _isBurning = value;
    }

    public Vector3 SpawnOffset
    {
        get => _spawnOffset;
        set => _spawnOffset = value;
    }

    public bool UseColliderCenter
    {
        get => _useColliderCenter;
        set => _useColliderCenter = value;
    }

    private void Start()
    {
        _isBurning = false;
    }

    // 스폰 위치 계산 (수정됨: Vector3 오프셋 지원)
    public Vector3 TaewooriPos()
    {
        if (_useColliderCenter)
        {
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"{gameObject.name}에 Collider가 없습니다! 오브젝트 위치 + 오프셋 사용");
                return transform.position + _spawnOffset;
            }

            // 콜라이더 중심을 기준으로 오프셋 적용
            Vector3 colCenter = col.bounds.center;
            Vector3 colExtents = col.bounds.extents;

            // Y축은 콜라이더 상단으로 설정
            Vector3 basePos = new Vector3(
                colCenter.x,
                colCenter.y + colExtents.y,
                colCenter.z
            );

            return basePos + _spawnOffset;
        }
        else
        {
            // 오브젝트 위치 + 오프셋
            return transform.position + _spawnOffset;
        }
    }

    void Update()
    {
        //잠깐 주석 쳐놓음 
        //if (_isBurning)
        //{
        //    _isBurning = false;
        //}
    }

    // 태우리 생성 위치를 시각적으로 표시 (디버그용)
    private void OnDrawGizmosSelected()
    {
        // 태우리 스폰 위치 계산
        Vector3 spawnPos = TaewooriPos();

        // 빨간색 구체로 태우리 위치 표시
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(spawnPos, 0.1f);

        // 노란색 선으로 오브젝트와 스폰 위치 연결
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, spawnPos);
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
