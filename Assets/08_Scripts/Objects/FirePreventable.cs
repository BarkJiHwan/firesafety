using System;
using System.Collections;
using UnityEngine;

public class FirePreventable : MonoBehaviour
{
    //예방 가능한 오브젝트
    [Header("true일 때 예방 완료"), Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isFirePreventable;

    [SerializeField] private GameObject _smokePrefab;
    [SerializeField] private GameObject _shieldPrefab;

    public bool IsFirePreventable
    {
        get => _isFirePreventable;
        set => _isFirePreventable = value;
    }
    private void Start()
    {
        _shieldPrefab.SetActive(false);
    }
    private void Update()
    {
        if (_isFirePreventable)
        {
            _shieldPrefab.SetActive(true);
        }
        else
        {
            _shieldPrefab.SetActive(false);
        }
    }
    public void CheckPreventFire()
    {

    }
}
