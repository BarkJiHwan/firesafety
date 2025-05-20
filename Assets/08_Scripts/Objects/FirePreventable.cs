using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class FirePreventable : MonoBehaviour
{
    //예방 가능한 오브젝트
    [Header("true일 때 예방 완료"), Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isFirePreventable;

    [SerializeField] private GameObject _smokePrefab;
    [SerializeField] private GameObject _shieldPrefab;

    [Header("임시 변수 추후 다른 스크립트에서 관리할 예정")]
    [SerializeField] private bool _isClickable = false;  // 예방 페이즈일 때만 true

    [SerializeField] private PreventableObjData _data;
    [SerializeField] private PreventTpye _myType;

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


    public bool IsFirePreventable
    {
        get => _isFirePreventable;
        set => _isFirePreventable = value;
    }
    private void Start()
    {
        ShowText(_myType);
        _smokePrefab.SetActive(false);
        _shieldPrefab.SetActive(false);
    }

    void OnMouseDown()
    {//마우스클릭 테스트 코드
        _isFirePreventable = !_isFirePreventable; // 상태 토글
    }
    void Update()
    {
        ApplySmokeSettings();
        ApplyShieldSettings();

        // 페이즈 확인
        var currentPhase = GameManager.Instance.CurrentPhase;
        _isClickable = currentPhase == GameManager.GamePhase.Prevention;

        if (_isClickable)
        {
            // 예방 페이즈
            if (_isFirePreventable)
            {
                _smokePrefab.SetActive(false);
                _shieldPrefab.SetActive(true);
            }
            else
            {
                _smokePrefab.SetActive(true);
                _shieldPrefab.SetActive(false);
            }
            //해당 오브젝트에 마우스를 올렸을 때 나타나야 하는 텍스트는?
        }
        else
        {
            _smokePrefab.SetActive(false);
        }
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

    public void ShowText(PreventTpye type)
    {
        string text = _data.GetText(type);
        Debug.Log(text + "테스트 텍스트");
        // 예: TextMeshProUGUI 등에 text를 할당
    }
}
