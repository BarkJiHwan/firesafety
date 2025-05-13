using System;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class FirePreventable : MonoBehaviour
{
    //예방 가능한 오브젝트
    [Header("true일 때 예방 완료"), Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isFirePreventable;

    [SerializeField] private GameObject _smokePrefab;
    [SerializeField] private GameObject _shieldPrefab;

    [Header("임시 변수 추후 다른 스크립트에서 관리할 예정")]
    [SerializeField] private bool _preventTime;

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
        IsFirePreventable = false;
    }
    private void Update()
    {
        ApplySmokeSettings();
        ApplyShieldSettings();
        if(_preventTime)
        {
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
}
