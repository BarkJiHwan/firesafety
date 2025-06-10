using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FirePreventable : MonoBehaviour
{
    //예방 가능한 오브젝트
    [Header("true일 때 예방 완료"), Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isFirePreventable;

    [SerializeField] private GameObject _smokePrefab;
    [SerializeField] private GameObject _shieldPrefab;

    [SerializeField] private PreventableObjData _data;
    [SerializeField] private PreventType _myType;

    [Serializable]
    public struct SmokeScaledAxis
    {
        [Range(0.1f, 2f)] public float x;
        [Range(0.1f, 2f)] public float y;
        [Range(0.1f, 2f)] public float z;
    }
    [Header("연기 오브젝트(파티클) 스캐일")]
    [SerializeField] private SmokeScaledAxis _smokeScale;

    [Header("쉴드 반지름")]
    [SerializeField, Range(0.1f, 2f)]
    private float _shieldRadius = 1f;

    //CHM - 소백이 연동 설정
    [Header("소백이 연동")]
    [SerializeField] private bool enableSobaekInteraction = true; // 소백이 상호작용 활성화

    Renderer _renderer;
    PhotonView _view;
    XRSimpleInteractable _xrInteractable;
    private bool _isXRinteract = true;
    public bool IsFirePreventable
    {
        get => _isFirePreventable;
        set => _isFirePreventable = value;
    }

    private void Start()
    {
        ApplySmokeSettings();
        ApplyShieldSettings();
        //CHM - XR 컴포넌트 가져오기
        _xrInteractable = GetComponent<XRSimpleInteractable>();
        
        //CHM - 소백이 상호작용 이벤트 자동 연결
        SetupSobaekInteraction();

        _view = GetComponent<PhotonView>();
        SetActivePrefab();

        // 예방 가능한 오브젝트에 새로운 Material 생성
        _renderer = GetComponent<Renderer>();
        Material[] arrMat = new Material[2];
        arrMat[0] = Resources.Load<Material>("Materials/OutlineMat");
        arrMat[1] = Resources.Load<Material>("Materials/OriginMat");
        _renderer.materials = arrMat;
        SetActiveOnMaterials(false);
    }

    //CHM - 소백이 상호작용 자동 설정 (싱글톤 방식으로 수정)
    private void SetupSobaekInteraction()
    {
        if (!enableSobaekInteraction)
            return;

        // XR Interactable이 없으면 추가
        if (_xrInteractable == null)
        {
            _xrInteractable = gameObject.AddComponent<XRSimpleInteractable>();
        }

        // 호버 이벤트 자동 연결 (소백이 찾기는 호버 시에 진행)
        _xrInteractable.hoverEntered.AddListener(OnSobaekHoverEnter);
        _xrInteractable.hoverExited.AddListener(OnSobaekHoverExit);


    }

    //CHM - 호버 시작 시 소백이 이동 (페이즈 무관)
    private void OnSobaekHoverEnter(HoverEnterEventArgs args)
    {
        if (Sobaek.Instance != null && enableSobaekInteraction)
        {
            Sobaek.Instance.MoveToInteractionTarget(transform);
        }
    }

    //CHM - 호버 종료 시 소백이 복귀 (페이즈 무관)
    private void OnSobaekHoverExit(HoverExitEventArgs args)
    {
        if (Sobaek.Instance != null && enableSobaekInteraction)
        {
            Sobaek.Instance.StopInteraction();
        }
    }

    //CHM - 소백이 상호작용 활성화/비활성화
    public void SetSobaekInteraction(bool enable)
    {
        enableSobaekInteraction = enable;

        if (!enable && Sobaek.Instance != null)
        {
            Sobaek.Instance.StopInteraction(); // 비활성화시 소백이 복귀
        }
    }

    void Update()
    {
        // 페이즈 확인
        var currentPhase = GameManager.Instance.CurrentPhase;

        if (currentPhase == GamePhase.Prevention)
        {
            if(_isXRinteract)
            {
                _xrInteractable.selectEntered.AddListener(EnterPrevention);
                _isXRinteract = false;
            }
            // 예방 페이즈
            if (_isFirePreventable)
            {
                OnFirePreventionComplete();
                // 예방 완료하면 Material 끄기
                SetActiveOnMaterials(false);
            }
            else
            {
                SetFirePreventionPending();
            }
        }

        // 예방 페이즈가 아닐때 Material이 켜져 있으면 끄기
        if (GameManager.Instance.CurrentPhase != GamePhase.Prevention)
        {
            if (isActiveOnMaterials())
            {
                SetActiveOnMaterials(false);
            }
        }
    }
    public void SetActivePrefab()
    {
        _smokePrefab.SetActive(false);
        _shieldPrefab.SetActive(false);
    }

    public void OnFirePreventionComplete()
    {
        _smokePrefab.SetActive(false);
        _shieldPrefab.SetActive(true);
    }

    public void SetFirePreventionPending()
    {
        _smokePrefab.SetActive(true);
        _shieldPrefab.SetActive(false);
    }

    //스모크 사이즈 셋팅
    private void ApplySmokeSettings()
    {
        _smokePrefab = Instantiate(_smokePrefab, transform.position, transform.rotation);
        _smokePrefab.transform.parent = transform;
            new Vector3(_smokeScale.x, _smokeScale.y, _smokeScale.z);
        _smokePrefab.transform.position = transform.position;
    }
    //쉴드 사이즈 셋팅
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

    public string ShowText()
    {
        // 예: TextMeshProUGUI 등에 text를 할당
        return _data.GetItem(_myType).Description;
    }

    public void EnterPrevention(SelectEnterEventArgs Args)
    {
        if (_view.IsMine)
        {
            if (!_isFirePreventable)
            {
                ++FireObjMgr.Instance.Count;
                _isFirePreventable = true;
                _view.RPC("CompleteFirePrevention", RpcTarget.AllBuffered, _isFirePreventable);
            }
            else
            {
                return;
            }
        }
    }

    public void SetActiveOnMaterials(bool isActive)
    {
        foreach (var mat in _renderer.materials)
        {
            mat.SetFloat("_isNearPlayer", isActive ? 1f : 0f);
        }
    }

    public void SetHighlightStronger(float interValue)
    {
        Material highlightMat = null;
        foreach (var mat in _renderer.materials)
        {
            if (mat.HasProperty("_RimPower"))
            {
                highlightMat = mat;
            }
        }
        float rimPower = Mathf.Lerp(2, -0.2f, interValue);
        highlightMat.SetFloat("_RimPower", rimPower);
    }

    bool isActiveOnMaterials()
    {
        float activeNum;
        bool isActive = false;
        foreach (var mat in _renderer.materials)
        {
            activeNum = mat.GetFloat("_isNearPlayer");
            // 체크표시가 켜져있으면
            if (activeNum == 1)
            {
                isActive = true;
                break;
            }
        }
        return isActive;
    }

    [PunRPC]
    public void CompleteFirePrevention(bool complete)
    {
        Debug.Log(_view.ViewID + "?");
        Debug.Log(PhotonNetwork.LocalPlayer + "누가누른건지 확인됨?" + "확인되네?");
        _isFirePreventable = complete;
    }

}
