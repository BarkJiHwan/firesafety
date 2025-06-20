using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.XR;
using Unity.VisualScripting;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit;

public enum EHandType
{
    RightHand = 0,
    LeftHand
}
[System.Serializable]
public class HandData
{
    public InputActionProperty triggerAction;
    public Transform grabSpot;
    public ParticleSystem normalFireFX;
    public ParticleSystem zeroAmountFireFX;
    public ParticleSystem initialFireFX;
    public GameObject modelPrefab;
    public bool initialFire = false;
    public bool enabled = false;
    public bool isSpraying = false;
    public EHandType handType;
    public XRRayInteractor interator;
}
public class FireSuppressantManager : MonoBehaviourPunCallbacks
{
    [Header("참조할 포톤 뷰")] public PhotonView pView;
    [Header("참조할 튜토리얼 소화기 스크립트")] public TutorialSuppressor tutoSuppressor;
    [Header("양손 소화기 데이터")]
    [SerializeField] public HandData _leftHand;
    [SerializeField] public HandData _rightHand;

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
    [SerializeField] private float _supplyCooldown;
    [SerializeField] private Transform _sprayOrigin; //스프레이 발사 지점
    [SerializeField] private int _currentAmount = 100;

    private readonly WaitForSeconds _checkTime = new(0.05f);
    private readonly WaitForSeconds _fireDelay = new(0.3f);
    private bool _isFeverTime = false;
    private readonly Collider[] _fireHits = new Collider[20];
    private readonly Collider[] _supplyHits = new Collider[10];
    private readonly Dictionary<Collider, IDamageable> _cacheds = new();
    private Dictionary<EHandType, HandData> _hands = new();
    private readonly Collider[] _checkingCols = new Collider[20];
    private Vector3 _sprayStartPos;
    private Vector3 _sprayEndPos;
    private float _triggerValue;
    [SerializeField] private bool _isPressed;
    private int _colHitCount;
    private int _fireHitCount;
    //Stopwatch stopwatch = new();
    private IEnumerator _currentCor;
    public static bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
    private HandData GetHand(EHandType type)
    {
        if (_hands.TryGetValue(type, out var hand))
        {
            return hand;
        }
        return null;
    }
    private IEnumerator Start()
    {
        while (SupplyManager.Instance == null)
        {
            yield return null;
        }
        if (tutoSuppressor != null)
        {
            _rightHand.interator = tutoSuppressor.rightHand.interator;
            _leftHand.interator = tutoSuppressor.leftHand.interator;
        }

        if (pView != null && pView.IsMine)
        {
            _hands[EHandType.LeftHand] = _leftHand;
            _hands[EHandType.RightHand] = _rightHand;
            SupplyManager.Instance.RegisterHand(EHandType.LeftHand, _leftHand, false);
            SupplyManager.Instance.RegisterHand(EHandType.RightHand, _rightHand, false);
            SupplyManager.Instance.suppressantManager = this;
            UnityEngine.Debug.Log("등록 완료 본게임");
        }

        if (GameManager.Instance.CurrentPhase == GamePhase.LeaveDangerArea)
        {
            enabled = false;
        }
    }
    private void Update()
    {
        if (!pView.IsMine)
        {
            return;
        }
        if (!GameManager.Instance.IsGameStart)
        {
            return;
        }
        ProcessHand(EHandType.RightHand);
        ProcessHand(EHandType.LeftHand);
        if (GameManager.Instance.CurrentPhase == GamePhase.Fever && !_isFeverTime)
        {
            FeverTimeOn();
        }
    }

    private void ProcessHand(EHandType type)
    {
        if (!pView.IsMine)
        {
            return;
        }
        var hand = GetHand(type);
        _triggerValue = hand.triggerAction.action.ReadValue<float>();
        _isPressed = _triggerValue > 0.1f;
        //_colHitCount = Physics.OverlapSphereNonAlloc(hand.grabSpot.position, _supplyDetectRange, _supplyHits, _supplyMask);
        //if (_isPressed && _colHitCounts > 0 && !_isFeverTime) <-- 본래 조건문
        //if (hand.interator.TryGetCurrent3DRaycastHit(out RaycastHit hit) && !_isFeverTime && hand.triggerAction.action.WasPressedThisFrame())
        //{
        //    if (IsInLayerMask(hit.collider.gameObject, _supplyMask))
        //    {
        //        Supply(type);
        //    }
        //}
        if (_isPressed && !hand.isSpraying && hand.enabled && hand.triggerAction.action.WasPressedThisFrame())
        {
            if (_currentCor == null)
            {
                _currentCor = SuppressingFire(type);
                StartCoroutine(_currentCor);
            }
            hand.isSpraying = true;
        }
        if (hand.triggerAction.action.WasReleasedThisFrame())
        {
            ResetSpray(type);
        }
    }
    private void FeverTimeOn()
    {
        if (!_isFeverTime)
        {
            _isFeverTime = true;
            _damage *= 2;
            _currentAmount = 100;
        }
    }

    //CHM 변경 
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
    //    // CHM: 마스터 클라이언트 체크 제거 - 각 클라이언트가 자신의 데미지 요청 처리
    //    for (int i = 0; i < _fireHitCount; i++)
    //    {
    //        var hit = _fireHits[i];
    //        if (!_cacheds.TryGetValue(hit, out var cached))
    //        {
    //            cached = hit.gameObject.GetComponent<IDamageable>();
    //            if (!_cacheds.ContainsKey(hit) && cached != null)
    //            {
    //                _cacheds[hit] = cached;
    //            }
    //        }
    //     }
    //
    //        // CHM: 네트워크 동기화를 위한 태우리 타입별 분기 처리 추가
    private void Spray(EHandType type)
    {
        var hand = GetHand(type);
        _sprayStartPos = _sprayOrigin.transform.position;
        _sprayEndPos = _sprayStartPos + (_sprayOrigin.forward * _sprayLength);

        _fireHitCount = Physics.OverlapCapsuleNonAlloc(_sprayStartPos, _sprayEndPos, _sprayRadius, _fireHits, _fireMask);
        if (!hand.normalFireFX.isPlaying)
        {
            hand.normalFireFX.Play();
            pView.RPC("RPC_PlayNormalFX", RpcTarget.Others, type);
        }
        pView.RPC(nameof(RPC_RequestDamage), RpcTarget.MasterClient, _sprayStartPos, _sprayEndPos);
        #region 포톤 마스터 클라이언트에게 요청 시키기 전
        //if (Photon.Pun.PhotonNetwork.IsMasterClient)
        //{
        //    for (int i = 0; i < _fireHitCount; i++)
        //    {
        //        var hit = _fireHits[i];
        //        if (!_cacheds.TryGetValue(hit, out var cached))
        //        {
        //            cached = hit.gameObject.GetComponent<IDamageable>();
        //            if (!_cacheds.ContainsKey(hit) && cached != null)
        //            {
        //                _cacheds[hit] = cached;
        //            }
        //        }
        //        cached?.TakeDamage(_damage);
        //    }
        //}
        #endregion
    }
    [PunRPC]
    private void RPC_RequestDamage(Vector3 start, Vector3 end, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }
        _fireHitCount = Physics.OverlapCapsuleNonAlloc(start, end, _sprayRadius, _fireHits, _fireMask);
        for (int i = 0; i < _fireHitCount; i++)
        {
            var hit = _fireHits[i];
            if (!_cacheds.TryGetValue(hit, out var cached))
            {
                cached = hit.gameObject.GetComponent<IDamageable>();
                if (cached != null)
                {
                    _cacheds[hit] = cached;
                }
            }
            //_cacheds[hit]?.TakeDamage(_damage);
            UnityEngine.Debug.Log("데미지 처리. 호출자: " + info.Sender);
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
    [PunRPC]
    private IEnumerator SuppressingFire(EHandType type)
    {
        if (!pView.IsMine)
        {
            yield break;
        }
        var hand = GetHand(type);
        if (!hand.initialFire && _currentAmount > 0)
        {
            hand.initialFireFX.Play();
            pView.RPC("RPC_PlayInitialFX", RpcTarget.Others, type);
            yield return _fireDelay;
            hand.initialFireFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            pView.RPC("RPC_StopPlayFX", RpcTarget.Others, type);
            hand.initialFire = true;
        }
        while (hand.triggerAction.action.ReadValue<float>() > 0)
        {
            if (_currentAmount > 0)
            {
                if (!_isFeverTime)
                {
                    _currentAmount -= _decreaseAmount;
                }
                Spray(type);
            }
            if (_currentAmount <= 0)
            {
                if (hand.normalFireFX.isPlaying)
                {
                    hand.normalFireFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    pView.RPC("RPC_StopPlayFX", RpcTarget.Others, type);
                }
                if (!hand.zeroAmountFireFX.isPlaying)
                {
                    hand.zeroAmountFireFX.Play();
                    pView.RPC("RPC_PlayZeroAmountFX", RpcTarget.Others, type);
                }
            }
            yield return _checkTime;
        }
    }
    private void ResetSpray(EHandType type)
    {
        if (!pView.IsMine)
        {
            return;
        }
        var hand = GetHand(type);
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
    public void Supply(EHandType type)
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
        if (!pView.IsMine || GameManager.Instance.CurrentPhase != GamePhase.Fire)
        {
            return;
        }
        var hand = GetHand(type);
        if (!_rightHand.enabled && !_leftHand.enabled)
        {
            hand.modelPrefab.SetActive(true);
            hand.enabled = true;
            _sprayOrigin = hand.modelPrefab.transform.Find("SprayOrigin");
            pView.RPC("RPC_SetActiveModel", RpcTarget.Others, type);
        }
        else if (!hand.enabled)
        {
            _rightHand.modelPrefab.SetActive(false);
            _leftHand.modelPrefab.SetActive(false);
            _rightHand.enabled = false;
            _leftHand.enabled = false;
            hand.modelPrefab.SetActive(true);
            hand.enabled = true;
        }
        if (hand.enabled && _currentAmount < _maxAmount)
        {
            _currentAmount = _maxAmount;
        }
    }
    public void DetachSuppressor()
    {
        if (_rightHand.enabled)
        {
            _rightHand.modelPrefab.SetActive(false);
            _rightHand.enabled = false;
        }
        else if (_leftHand.enabled)
        {
            _leftHand.modelPrefab.SetActive(false);
            _leftHand.enabled = false;
        }
        _currentAmount = _maxAmount;
    }
    private void OnDrawGizmos()
    {
        DrawSprayRange(_leftHand);
        DrawSprayRange(_rightHand);
    }
    public void SetAmountZero() => _currentAmount = 0;
    [PunRPC]
    private void RPC_PlayInitialFX(EHandType type)
    {
        var hand = GetHand(type);
        hand.initialFireFX.Play();
    }
    [PunRPC]
    private void RPC_PlayNormalFX(EHandType type)
    {
        var hand = GetHand(type);
        hand.normalFireFX.Play();
    }
    [PunRPC]
    private void RPC_PlayZeroAmountFX(EHandType type)
    {
        var hand = GetHand(type);
        hand.zeroAmountFireFX.Play();
    }
    [PunRPC]
    private void RPC_StopPlayFX(EHandType type)
    {
        var hand = GetHand(type);
        if (hand.initialFireFX.isPlaying)
        {
            hand.initialFireFX.Stop();
        }
        if (hand.normalFireFX.isPlaying)
        {
            hand.normalFireFX.Stop();
        }
        if (hand.zeroAmountFireFX.isPlaying)
        {
            hand.zeroAmountFireFX.Stop();
        }
    }
    [PunRPC]
    private void RPC_SetActiveModel(EHandType type)
    {
        var hand = GetHand(type);
        if (!hand.modelPrefab.activeSelf)
        {
            hand.modelPrefab.SetActive(true);
        }
    }
    [PunRPC]
    private void RPC_SetActiveModelFalse()
    {
        if (!_rightHand.enabled && _rightHand.modelPrefab.activeSelf)
        {
            _rightHand.modelPrefab.SetActive(false);
        }
        if (!_leftHand.enabled && _leftHand.modelPrefab.activeSelf)
        {
            _leftHand.modelPrefab.SetActive(false);
        }
    }

    private void DrawSprayRange(HandData hand)
    {
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
