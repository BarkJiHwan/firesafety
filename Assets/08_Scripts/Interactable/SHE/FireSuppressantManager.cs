using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.XR;
using Unity.VisualScripting;
using Photon.Pun;
[System.Serializable]
public class HandData
{
    //public string handName; // 디버그용
    public InputActionProperty triggerAction;
    public Transform grabSpot;
    public ParticleSystem normalFireFX;
    public ParticleSystem zeroAmountFireFX;
    public ParticleSystem initialFireFX;
    public GameObject modelPrefab;
    public bool initialFire = false;
    public bool enabled = false;
    public bool isSpraying = false;
}
public class FireSuppressantManager : MonoBehaviour
{
    [Header("양손 소화기 데이터")]
    [SerializeField] private HandData _leftHand;
    [SerializeField] private HandData _rightHand;

    [Header("공통 설정")]
    [SerializeField] private float _sprayLength = 2.5f;
    [SerializeField] private float _sprayRadius = 1;
    [SerializeField] private int _damage = 1;
    [SerializeField] private int _maxAmount = 100;
    [SerializeField] private int _decreaseAmount = 1;
    [SerializeField] private LayerMask _fireMask;
    [SerializeField] private float _refillCooldown = 3f;
    [SerializeField] private LayerMask _supplyMask;
    [SerializeField] private float _supplyDetectRange = 0.8f;
    [SerializeField] private bool _isFeverTime;
    [SerializeField] private float _supplyCooldown;
    [SerializeField] private Transform _sprayOrigin; //스프레이 발사 지점
    [SerializeField] private int _currentAmount = 100;

    private readonly WaitForSeconds _checkTime = new(0.05f);
    private readonly WaitForSeconds _fireDelay = new(0.3f);

    private readonly Collider[] _fireHits = new Collider[20];
    private readonly Collider[] _supplyHits = new Collider[10];
    private readonly Dictionary<Collider, IDamageable> _cacheds = new();
    private readonly Collider[] _checkingCols = new Collider[20];
    private Vector3 _sprayStartPos;
    private Vector3 _sprayEndPos;
    private float _triggerValue;
    [SerializeField] private bool _isPressed;
    private int _colHitCount;
    private int _fireHitCount;
    //Stopwatch stopwatch = new();
    private IEnumerator _currentCor;
    private HandData _currentHand;
    private void Update()
    {
        ProcessHand(_rightHand);
        ProcessHand(_leftHand);
        if (_supplyCooldown > 0)
        {
            _supplyCooldown -= Time.deltaTime;
        }
        //향후 게임 매니저와 연계
    }

    private void ProcessHand(HandData hand)
    {
        _triggerValue = hand.triggerAction.action.ReadValue<float>();
        _isPressed = _triggerValue > 0.1f;
        if (!_isFeverTime)
        {
            _colHitCount = Physics.OverlapSphereNonAlloc(hand.grabSpot.position, _supplyDetectRange, _supplyHits, _supplyMask);
        }
        //if (_isPressed && _colHitCounts > 0 && !_isFeverTime) <-- 본래 조건문
        if (_colHitCount > 0 && !_isFeverTime)//테스트용
        {
            Supply(hand);
            _supplyCooldown = _refillCooldown;
        }
        if (_isPressed && !hand.isSpraying && hand.enabled && hand.triggerAction.action.WasPressedThisFrame())
        {
            if (_currentCor == null)
            {
                _currentCor = SuppressingFire(hand);
                StartCoroutine(_currentCor);
            }
            hand.isSpraying = true;
        }
        if (hand.triggerAction.action.WasReleasedThisFrame())
        {
            ResetSpray(hand);
        }
        if (GameManager.Instance.CurrentPhase == GamePhase.Fever)
        {
            if (!_isFeverTime)
            {
                _isFeverTime = true;
                FeverTimeOn(hand);
            }
        }
    }

    //private void Spray(HandData hand)
    //{
    //    _sprayStartPos = _sprayOrigin.transform.position;
    //    _sprayEndPos = _sprayStartPos + (_sprayOrigin.forward * _sprayLength);
    //
    //    _fireHitCount = Physics.OverlapCapsuleNonAlloc(_sprayStartPos, _sprayEndPos, _sprayRadius, _fireHits, _fireMask);
    //    if (!hand.normalFireFX.isPlaying)
    //    {
    //        hand.normalFireFX.Play();
    //    }
    //
    //    if (Photon.Pun.PhotonNetwork.IsMasterClient)
    //    {
    //        for (int i = 0; i < _fireHitCount; i++)
    //        {
    //            var hit = _fireHits[i];
    //            if (!_cacheds.TryGetValue(hit, out var cached))
    //            {
    //                cached = hit.gameObject.GetComponent<IDamageable>();
    //                if (!_cacheds.ContainsKey(hit) && cached != null)
    //                {
    //                    _cacheds[hit] = cached;
    //                }
    //            }
    //            cached?.TakeDamage(_damage);
    //        }
    //    }
    //}

    //CHM 변경 
    private void Spray(HandData hand)
    {
        _sprayStartPos = _sprayOrigin.transform.position;
        _sprayEndPos = _sprayStartPos + (_sprayOrigin.forward * _sprayLength);
    
        _fireHitCount = Physics.OverlapCapsuleNonAlloc(_sprayStartPos, _sprayEndPos, _sprayRadius, _fireHits, _fireMask);
        if (!hand.normalFireFX.isPlaying)
        {
            hand.normalFireFX.Play();
        }
    
        // CHM: 마스터 클라이언트 체크 제거 - 각 클라이언트가 자신의 데미지 요청 처리
        for (int i = 0; i < _fireHitCount; i++)
        {
            var hit = _fireHits[i];
            if (!_cacheds.TryGetValue(hit, out var cached))
            {
                cached = hit.gameObject.GetComponent<IDamageable>();
                if (!_cacheds.ContainsKey(hit) && cached != null)
                {
                    _cacheds[hit] = cached;
                }
            }
    
            // CHM: 네트워크 동기화를 위한 태우리 타입별 분기 처리 추가
            if (cached != null)
            {
                // CHM: 태우리 타입인지 확인하고 네트워크 데미지 요청
                if (cached is Taewoori taewoori)
                {
                    taewoori.RequestDamageFromClient(_damage);
                }
                // CHM: 스몰태우리 타입인지 확인하고 네트워크 데미지 요청
                else if (cached is SmallTaewoori smallTaewoori)
                {
                    smallTaewoori.RequestDamageFromClient(_damage);
                }
                // CHM: 일반 IDamageable 오브젝트는 기존 방식으로 데미지 처리
                else
                {
                    // 일반 오브젝트는 마스터만 처리
                    if (Photon.Pun.PhotonNetwork.IsMasterClient)
                    {
                        cached.TakeDamage(_damage);
                    }
                }
            }
        }
    }
    private IEnumerator SuppressingFire(HandData hand)
    {
        if (!hand.initialFire && _currentAmount > 0)
        {
            hand.initialFireFX.Play();
            yield return _fireDelay;
            hand.initialFireFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            hand.initialFire = true;
        }
        while (hand.triggerAction.action.ReadValue<float>() > 0)
        {
            if (_currentAmount > 0)
            {
                if (_currentAmount > 0 && !_isFeverTime)
                {
                    _currentAmount -= _decreaseAmount;
                }
                Spray(hand);
            }
            if (_currentAmount <= 0)
            {
                if (hand.normalFireFX.isPlaying)
                {
                    hand.normalFireFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                if (!hand.zeroAmountFireFX.isPlaying)
                {
                    hand.zeroAmountFireFX.Play();
                }
            }
            yield return _checkTime;
        }
    }
    private void ResetSpray(HandData hand)
    {
        if (hand.normalFireFX.isPlaying)
        {
            hand.normalFireFX.Stop();
        }
        if (hand.zeroAmountFireFX.isPlaying)
        {
            hand.zeroAmountFireFX.Stop();
        }
        if (hand.initialFireFX.isPlaying)
        {
            hand.initialFireFX.Stop();
        }
        _cacheds.Clear();
        hand.isSpraying = false;
        hand.initialFire = false;
        _currentCor = null;
    }
    private void Supply(HandData hand)
    {
        #region Instantiate ver
        //if (!_rightHand.enabled && !_leftHand.enabled)
        //{
        //    Quaternion sprayRot = Quaternion.EulerAngles(-90f, 0f, -90f);
        //    Vector3 sprayPos = new(0f, 0.008f, 0f);
        //    Quaternion finalRot = hand.grabSpot.rotation * sprayRot;
        //    Vector3 finalPos = hand.grabSpot.position + hand.grabSpot.rotation * sprayPos;
        //    var spray = Instantiate(hand.modelPrefab, finalPos, finalRot, hand.grabSpot);
        //    hand.enabled = true;
        //    _sprayOrigin = spray.transform.Find("SprayOrigin");
        //    hand.normalFireFX = spray.transform.Find("Normal FX").GetComponent<ParticleSystem>();
        //    hand.zeroAmountFireFX = spray.transform.Find("Zero Amount FX").GetComponent<ParticleSystem>();
        //    hand.initialFireFX = spray.transform.Find("Initialize FX").GetComponent<ParticleSystem>();
        //    Debug.Log("보급: 생성 및 할당");
        //}
        #endregion
        if (!_rightHand.enabled && !_leftHand.enabled)
        {
            hand.modelPrefab.SetActive(true);
            hand.enabled = true;
            _sprayOrigin = hand.modelPrefab.transform.Find("SprayOrigin");
        }
        else if (_currentHand != hand)
        {
            _rightHand.modelPrefab.SetActive(false);
            _leftHand.modelPrefab.SetActive(false);
            _rightHand.enabled = false;
            _leftHand.enabled = false;
            hand.modelPrefab.SetActive(true);
            hand.enabled = true;
            _currentHand = hand;
        }
        if (hand.enabled && _currentAmount < 600)
        {
            _currentAmount = _maxAmount;
        }
    }
    private void FeverTimeOn(HandData hand)
    {
        _damage *= 2;
        _currentAmount = _maxAmount;
    }
    private void OnDrawGizmos()
    {
        DrawSprayRange(_leftHand);
        DrawSprayRange(_rightHand);
    }
    public void SetAmountZero() => _currentAmount = 0;

    private void DrawSprayRange(HandData hand)
    {
        //보급 인지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(hand.grabSpot.position, _supplyDetectRange);
        //소화기 범위
        if (_sprayOrigin == null)
        {
            return;
        }
        Gizmos.color = Color.cyan;
        Vector3 start = _sprayOrigin.position;
        Vector3 end = start + (_sprayOrigin.forward * _sprayLength);
        Gizmos.DrawWireSphere(start, _sprayRadius);
        Gizmos.DrawWireSphere(end, _sprayRadius);
        Gizmos.DrawLine(start, end);
    }
}
