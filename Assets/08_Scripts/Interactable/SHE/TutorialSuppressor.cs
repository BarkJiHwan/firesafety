using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

public class TutorialSuppressor : MonoBehaviourPunCallbacks
{
    [Header("참조할 포톤 뷰")] public PhotonView pView;
    public HandData leftHand;
    public HandData rightHand;
    [SerializeField] private float _sprayLength = 2.5f;
    [SerializeField] private float _sprayRadius = 1;
    [SerializeField] private int _damage = 1;
    [SerializeField] private int _maxAmount = 100;
    [SerializeField] private LayerMask _fireMask;
    [SerializeField] private LayerMask _supplyMask;
    [SerializeField] private float _supplyDetectRange = 0.8f;
    [SerializeField] private Transform _sprayOrigin; //스프레이 발사 지점
    [SerializeField] private int _currentAmount = 100;

    private readonly WaitForSeconds _checkTime = new(0.05f);
    private readonly WaitForSeconds _fireDelay = new(0.3f);
    private readonly Collider[] _fireHits = new Collider[20];
    private readonly Collider[] _supplyHits = new Collider[10];
    private readonly Dictionary<Collider, IDamageable> _cacheds = new();
    private readonly Collider[] _checkingCols = new Collider[20];
    private Dictionary<EHandType, HandData> _hands = new();
    private Vector3 _sprayStartPos;
    private Vector3 _sprayEndPos;
    private float _triggerValue;
    private int _colHitCount;
    private int _fireHitCount;
    private IEnumerator _currentCor;
    private bool _isPressed;
    private HandData GetHand(EHandType type)
    {
        if (_hands.TryGetValue(type, out var hand))
        {
            return hand;
        }
        return null;
    }
    //기본적인 구성은 모두 본게임 소화기와 동일합니다
    private IEnumerator Start()
    {
        while (SupplyManager.Instance == null)
        {
            yield return null;
        }

        if (pView != null && pView.IsMine)
        {
            _hands[EHandType.LeftHand] = leftHand;
            _hands[EHandType.RightHand] = rightHand;
            SupplyManager.Instance.RegisterHand(EHandType.LeftHand, leftHand, true);
            SupplyManager.Instance.RegisterHand(EHandType.RightHand, rightHand, true);
            SupplyManager.Instance.tutorialSuppressor = this;
            UnityEngine.Debug.Log("등록 완료 튜토리얼");
        }

        if (GameManager.Instance.CurrentPhase == GamePhase.LeaveDangerArea)
        {
            enabled = false;
        }
    }
    private void Update()
    {
        ProcessHand(leftHand);
        ProcessHand(rightHand);
        if (GameManager.Instance.IsGameStart)
        {
            DetachSuppressor();
            enabled = false;
        }
    }
    private void ProcessHand(HandData hand)
    {
        _triggerValue = hand.triggerAction.action.ReadValue<float>();
        _isPressed = _triggerValue > 0.1f;
        _colHitCount = Physics.OverlapSphereNonAlloc(hand.grabSpot.position, _supplyDetectRange, _supplyHits, _supplyMask);
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
    }
    private void Spray(HandData hand)
    {
        _sprayStartPos = _sprayOrigin.transform.position;
        _sprayEndPos = _sprayStartPos + (_sprayOrigin.forward * _sprayLength);
        _fireHitCount = Physics.OverlapCapsuleNonAlloc(_sprayStartPos, _sprayEndPos, _sprayRadius, _fireHits, _fireMask);
        if (!hand.normalFireFX.isPlaying)
        {
            hand.normalFireFX.Play();
        }
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
            cached?.TakeDamage(_damage);
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
    //public void Supply(EHandType type)
    //{
    //      if (_currentAmount <= 0)
    //      {
    //          TutorialDataMgr.Instance.IsTriggerSupply = true;
    //      }
    //      if (!_rightHand.enabled && !_leftHand.enabled)
    //      {
    //          hand.modelPrefab.SetActive(true);
    //          hand.enabled = true;
    //          _sprayOrigin = hand.modelPrefab.transform.Find("SprayOrigin");
    //      }
    //      else if (!hand.enabled)
    //      {
    //          _rightHand.modelPrefab.SetActive(false);
    //          _leftHand.modelPrefab.SetActive(false);
    //          _rightHand.enabled = false;
    //          _leftHand.enabled = false;
    //          hand.modelPrefab.SetActive(true);
    //          hand.enabled = true;
    //      }
    //      if (hand.enabled && _currentAmount < _maxAmount)
    //      {
    //          _currentAmount = _maxAmount;
    //      }
    //}
  public void DetachSuppressor()
    {
        if (rightHand.enabled)
        {
            rightHand.modelPrefab.SetActive(false);
            rightHand.enabled = false;
        }
        if (leftHand.enabled)
        {
            leftHand.modelPrefab.SetActive(false);
            leftHand.enabled = false;
        }
        _currentAmount = _maxAmount;
    }
    public void SetAmountZero() => _currentAmount = 0;
    public void Supply(EHandType type)
    {
        if (!pView.IsMine)
        {
            return;
        }
        var hand = GetHand(type);
        if (_currentAmount <= 0)
        {
            TutorialDataMgr.Instance.IsTriggerSupply = true;
        }
        if (!hand.enabled)
        {
            if (rightHand != hand)
            {
                rightHand.modelPrefab.SetActive(false);
                rightHand.enabled = false;
            }
            if (leftHand != hand)
            {
                leftHand.modelPrefab.SetActive(false);
                leftHand.enabled = false;
            }
            hand.modelPrefab.SetActive(true);
            hand.enabled = true;
            _sprayOrigin = hand.modelPrefab.transform.Find("SprayOrigin");
        }
        if (hand.enabled && _currentAmount < _maxAmount)
        {
            _currentAmount = _maxAmount;
        }
    }

}
