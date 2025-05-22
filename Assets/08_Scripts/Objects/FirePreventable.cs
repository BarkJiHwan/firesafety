using System;
using System.Collections;
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

    Renderer _renderer;

    public bool IsFirePreventable
    {
        get => _isFirePreventable;
        set => _isFirePreventable = value;
    }
    private void Start()
    {
        GetComponent<XRSimpleInteractable>().activated.AddListener(EnterPrevention);
        _smokePrefab.SetActive(false);
        _shieldPrefab.SetActive(false);

        // 예방 가능한 오브젝트에 새로운 Material 생성
        _renderer = GetComponent<Renderer>();
        Material[] arrMat = new Material[2];
        arrMat[0] = Resources.Load<Material>("Materials/OutlineMat");
        arrMat[1] = Resources.Load<Material>("Materials/OriginMat");
        _renderer.materials = arrMat;
        SetActiveOnMaterials(false);
    }

    void Update()
    {
        ApplySmokeSettings();
        ApplyShieldSettings();

        // 페이즈 확인
        var currentPhase = GameManager.Instance.CurrentPhase;

        if (currentPhase == GamePhase.Prevention)
        {
            // 예방 페이즈
            if (_isFirePreventable)
            {
                OnFirePreventionComplete();
            }
            else
            {
                SetFirePreventionPending();
            }
        }
        else
        {
            _smokePrefab.SetActive(false);
        }
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
    private void ApplySmokeSettings() => _smokePrefab.transform.localScale =
            new Vector3(_smokeScale.x, _smokeScale.y, _smokeScale.z);
    //쉴드 사이즈 셋팅
    private void ApplyShieldSettings()
    {
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

    public void EnterPrevention(ActivateEventArgs args)
    {
        if(!_isFirePreventable)
        {
            _isFirePreventable = true;
        }
        else
        {
            return;
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
}
