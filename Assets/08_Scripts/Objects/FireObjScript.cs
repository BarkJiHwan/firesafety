using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireObjScript : MonoBehaviour, ITaewooriPos
{
    [Header("true일 때 태우리 생성 가능 상태"), Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isBurning;

    [Header("태우리 스폰 위치 설정")]
    [Tooltip("태우리 생성 위치에 추가할 오프셋 (x, y, z)")]
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.1f, 0f);

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

    private void Start()
    {
        _isBurning = false;
    }

    // 스폰 위치 계산 (수정됨: 항상 콜라이더 상단 + 오프셋 사용)
    public Vector3 TaewooriPos()
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

    void Update()
    {
        //잠깐 주석 쳐놓음 
        //if (_isBurning)
        //{
        //    _isBurning = false;
        //}
    }

    // 태우리 생성 위치를 시각적으로 표시 (디버그용)
    private void OnDrawGizmos()
    {
        // 태우리 스폰 위치 계산
        Vector3 spawnPos = TaewooriPos();

        // 오프셋이 0이 아니면 파란색, 그렇지 않으면 빨간색으로 표시
        if (_spawnOffset != Vector3.zero)
        {
            Gizmos.color = Color.blue;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        // 태우리 위치에 구체 그리기
        Gizmos.DrawSphere(spawnPos, 0.1f);

        // 라인 그리기 (같은 색상 유지)
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
