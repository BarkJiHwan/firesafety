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

    public bool IsFirePreventable
    {
        get => _isFirePreventable;
        set => _isFirePreventable = value;
    }
    private void Start()
    {
        IsFirePreventable = false;
        SmokeInstantiateAsChildWithTransform();
        ShieldInstantiateAsChildWithTransform();
    }
    private void Update()
    {
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

    //게임 시작 스모크(파이클)생성 및 셋팅하는 메서드
    private void SmokeInstantiateAsChildWithTransform()
    {
        GameObject smoke = Instantiate(_smokePrefab);
        smoke.transform.parent = transform;
        smoke.transform.position = transform.position;
        smoke.transform.localScale = new Vector3(1, 1, 1);
        _smokePrefab = smoke;
        _smokePrefab.SetActive(false);
    }
    //게임 시작 쉴드(오브젝트)생성 및 셋팅하는 메서드
    private void ShieldInstantiateAsChildWithTransform()
    {
        GameObject shield = Instantiate(_shieldPrefab);
        shield.transform.parent = transform;
        shield.transform.position = transform.position;
        shield.transform.localScale = new Vector3(2, 2, 2);
        _shieldPrefab = shield;
        _shieldPrefab.SetActive(false);
    }
}
