using UnityEngine;

public class FireObjScript : MonoBehaviour, ITaewooriPos
{
    [Header("true일 때 태우리 생성 가능 상태")]
    [SerializeField] private bool _isBurning;

    [Header("태우리 스폰 위치 및 회전 설정")]
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private Vector3 _spawnRotation = new Vector3(0f, 0f, 0f); // 태우리 회전값

    // 상태 변경 이벤트 정의
    public delegate void BurningStateChangedHandler(FireObjScript fireObj, bool newState);
    public event BurningStateChangedHandler OnBurningStateChanged;

    private Taewoori _activeTaewoori = null;

    public bool IsBurning
    {
        get => _isBurning;
        set
        {
            if (_isBurning != value)
            {
                _isBurning = value;

                // 상태 변경 이벤트 발생
                OnBurningStateChanged?.Invoke(this, _isBurning);

                // 불이 켜지면 태우리 생성 (이미 생성된 태우리가 없을 때만)
                if (_isBurning && !HasActiveTaewoori())
                {
                    if (TaewooriPoolManager.Instance != null)
                    {
                        TaewooriPoolManager.Instance.SpawnTaewooriAtPosition(TaewooriPos(), this);
                    }
                }
            }
        }
    }

    public Vector3 SpawnOffset
    {
        get => _spawnOffset;
        set => _spawnOffset = value;
    }

    public Vector3 SpawnRotation
    {
        get => _spawnRotation;
        set => _spawnRotation = value;
    }

    private void Start()
    {
        // 초기 상태 설정
        _isBurning = false;
    }

    // 오브젝트 위치 + 설정한 오프셋
    public Vector3 TaewooriPos()
    {
        return transform.position + _spawnOffset;
    }

    // 스폰 회전 계산
    public Quaternion TaewooriRotation()
    {
        // 설정된 회전값 그대로 사용
        return Quaternion.Euler(_spawnRotation);
    }

    // 태우리 참조 관련 메서드
    public void SetActiveTaewoori(Taewoori taewoori)
    {
        _activeTaewoori = taewoori;
    }

    public void ClearActiveTaewoori()
    {
        _activeTaewoori = null;
    }

    public bool HasActiveTaewoori()
    {
        return _activeTaewoori != null && _activeTaewoori.gameObject.activeInHierarchy;
    }

    private void OnDrawGizmos()
    {
        // 태우리 스폰 위치 계산
        Vector3 spawnPos = TaewooriPos();
        Quaternion spawnRot = TaewooriRotation();

        // 오프셋이 0이 아니면 파란색, 그렇지 않으면 빨간색으로 표시
        Gizmos.color = (_spawnOffset != Vector3.zero) ? Color.blue : Color.red;

        // 태우리 위치에 구체 그리기
        Gizmos.DrawSphere(spawnPos, 0.1f);

        // 라인 그리기 (같은 색상 유지)
        Gizmos.DrawLine(transform.position, spawnPos);

        // 회전 방향 표시 (화살표)
        Vector3 forward = spawnRot * Vector3.forward * 0.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(spawnPos, spawnPos + forward); // 전방 방향

        // 화살표 끝부분
        Vector3 arrowHead1 = spawnPos + forward - (spawnRot * Vector3.forward * 0.1f) + (spawnRot * Vector3.right * 0.05f);
        Vector3 arrowHead2 = spawnPos + forward - (spawnRot * Vector3.forward * 0.1f) - (spawnRot * Vector3.right * 0.05f);
        Gizmos.DrawLine(spawnPos + forward, arrowHead1);
        Gizmos.DrawLine(spawnPos + forward, arrowHead2);
    }
}
