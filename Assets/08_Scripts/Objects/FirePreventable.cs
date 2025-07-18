using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// 예방 가능한 오브젝트 관련 변수 및 기능을 관리하는 컴포넌트
public partial class FirePreventable : MonoBehaviour
{
    // 예방 완료 여부 플래그
    [Header("true일 때 예방 완료"), Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isFirePreventable;

    // 연기(스모크) 프리팹 오브젝트
    [SerializeField] private GameObject _smokePrefab;

    // 쉴드 프리팹 오브젝트
    [SerializeField] private GameObject _shieldPrefab;

    // 예방 오브젝트의 데이터(설명, 속성 등)
    [SerializeField] private PreventableObjData _data;

    // 이 오브젝트의 예방 타입(예: 전선, 소화기 등)
    [SerializeField] private PreventType _myType;

    /// <summary>
    /// 연기(스모크) 오브젝트의 스케일 값을 저장하는 구조체
    /// </summary>
    [Serializable]
    private struct SmokeScaledAxis
    {
        [Range(0.1f, 2f)] public float x;
        [Range(0.1f, 2f)] public float y;
        [Range(0.1f, 2f)] public float z;
    }

    // 연기 오브젝트(파티클)의 스케일 값
    [Header("연기 오브젝트(파티클) 스캐일")]
    [SerializeField] private SmokeScaledAxis _smokeScale;

    // 쉴드 반지름 값
    [Header("쉴드 반지름")]
    [SerializeField, Range(0.1f, 2f)]
    private float _shieldRadius = 1f;

    // 렌더러 및 머티리얼 관련 변수들
    Renderer _renderer;                 // 오브젝트의 렌더러
    Material[] originMats;              // 원본 머티리얼 배열
    Material[] arrMat;                  // 변경용 머티리얼 배열
    Material[] originChildMats;         // 자식 오브젝트의 원본 머티리얼 배열

    PhotonView _view;                   // Photon 네트워크 동기화용 뷰
    XRSimpleInteractable _xrInteractable; // XR 인터랙션 컴포넌트
    private bool _isXRinteract = true;  // XR 인터랙션 등록 여부

    /// <summary>
    /// 예방 완료 여부 프로퍼티
    /// </summary>
    public bool IsFirePreventable
    {
        get => _isFirePreventable;
        set => _isFirePreventable = value;
    }

    /// <summary>
    /// 이 오브젝트의 예방 타입 반환
    /// </summary>
    public PreventType MyType
    {
        get => _myType;
    }

    private void Start()
    {
        // 연기 및 쉴드 오브젝트 초기화
        ApplySmokeSettings();
        ApplyShieldSettings();
        _xrInteractable = GetComponent<XRSimpleInteractable>();
        _view = GetComponent<PhotonView>();

        // 연기 및 쉴드 비활성화
        SetActiveOut();

        // CYW - 새로운 머티리얼 생성 및 적용
        SetMaterial();
        ChangeMaterial(gameObject);

        // 자식 오브젝트가 있고, 타입이 OldWire가 아니면 자식에도 머티리얼 적용
        if (isHaveChild && _myType != PreventType.OldWire)
        {
            ChangeMaterial(transform.GetChild(0).gameObject);
        }

        // 게임 페이즈 변경 이벤트 구독
        GameManager.Instance.OnPhaseChanged += OnSetUIAction;
    }

    void Update()
    {
        // 현재 게임 페이즈 확인
        var currentPhase = GameManager.Instance.CurrentPhase;

        if (currentPhase == GamePhase.Prevention)
        {
            // XR 인터랙션 리스너 1회 등록
            if (_isXRinteract)
            {
                _xrInteractable.selectEntered.AddListener(EnterPrevention);
                _isXRinteract = false;
            }
            // 예방 페이즈 처리
            if (_isFirePreventable)
            {
                OnFirePreventionComplete();        // 예방 완료 처리
                SetActiveOnMaterials(false);       // 머티리얼 비활성화
                MakeExceptObjectOff();             // 예외 오브젝트 처리
            }
            else
            {
                SetFirePreventionPending();        // 예방 대기 상태 처리
            }
        }
    }

    /// <summary>
    /// 연기와 쉴드 오브젝트를 모두 비활성화합니다.
    /// </summary>
    public void SetActiveOut()
    {
        _smokePrefab.SetActive(false);
        _shieldPrefab.SetActive(false);
    }

    /// <summary>
    /// 예방 완료 시 호출. 연기는 끄고 쉴드는 켭니다.
    /// </summary>
    public void OnFirePreventionComplete()
    {
        _smokePrefab.SetActive(false);
        _shieldPrefab.SetActive(true);
    }

    /// <summary>
    /// 예방 대기 상태 처리. 연기는 켜고 쉴드는 끕니다.
    /// </summary>
    public void SetFirePreventionPending()
    {
        _smokePrefab.SetActive(true);
        _shieldPrefab.SetActive(false);
    }

    /// <summary>
    /// 연기 오브젝트만 비활성화
    /// </summary>
    public void SomkePrefabActiveOut()
    {
        _smokePrefab.SetActive(false);
    }

    /// <summary>
    /// 연기(스모크) 오브젝트를 생성하고, 스케일을 적용합니다.
    /// </summary>
    private void ApplySmokeSettings()
    {
        _smokePrefab = Instantiate(_smokePrefab, transform.position, transform.rotation);
        _smokePrefab.transform.parent = transform;
        _smokePrefab.transform.localScale = new Vector3(_smokeScale.x, _smokeScale.y, _smokeScale.z);
        _smokePrefab.transform.position = transform.position;
    }

    /// <summary>
    /// 쉴드 오브젝트를 생성하고, 반지름(스케일)을 적용합니다.
    /// </summary>
    private void ApplyShieldSettings()
    {
        _shieldPrefab = Instantiate(_shieldPrefab, transform.position, transform.rotation);
        _shieldPrefab.transform.parent = transform;
        float diameter = _shieldRadius;

        _shieldPrefab.transform.localScale =
                new Vector3(diameter / transform.localScale.x
                , diameter / transform.localScale.y
                , diameter / transform.localScale.z);
    }

    /// <summary>
    /// 오브젝트 설명 텍스트 반환(예: UI 표시용)
    /// </summary>
    public string ShowText()
    {
        return _data.GetItem(_myType).Description;
    }

    /// <summary>
    /// XR 인터랙션으로 예방을 시도할 때 호출되는 메서드
    /// </summary>
    /// <param name="Args">XR Select 이벤트 인자</param>
    public void EnterPrevention(SelectEnterEventArgs Args)
    {
        if (!_isFirePreventable)
        {
            ++FireObjMgr.Instance.Count;   // 예방 카운트 증가
            _isFirePreventable = true;
            _view.RPC("CompleteFirePrevention", RpcTarget.AllBuffered, _isFirePreventable); // 네트워크 동기화
        }
        else
        {
            return;
        }
    }

    /// <summary>
    /// 네트워크를 통해 예방 완료 상태를 동기화합니다.
    /// </summary>
    /// <param name="complete">예방 완료 여부</param>
    [PunRPC]
    public void CompleteFirePrevention(bool complete)
    {
        _isFirePreventable = complete;
    }
}
