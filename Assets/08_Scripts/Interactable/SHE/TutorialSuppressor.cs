using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class TutorialSuppressor : MonoBehaviour
{
    [SerializeField] HandData _leftHand;
    [SerializeField] HandData _rightHand;
    [SerializeField] private float _sprayLength = 2.5f;
    [SerializeField] private float _sprayRadius = 1;
    [SerializeField] private int _damage = 1;
    [SerializeField] private int _maxAmount = 100;
    [SerializeField] private int _decreaseAmount = 1;
    [SerializeField] private LayerMask _fireMask;
    [SerializeField] private float _refillCooldown = 3f;
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
    private Vector3 _sprayStartPos;
    private Vector3 _sprayEndPos;
    private float _triggerValue;
    private int _colHitCount;
    private int _fireHitCount;
    private IEnumerator _currentCor;
    private bool _isPressed;

    private void Update()
    {
        ProcessHand(_leftHand);
        ProcessHand(_rightHand);
    }
    private void ProcessHand(HandData hand)
    {
        _triggerValue = hand.triggerAction.action.ReadValue<float>();
        _isPressed = _triggerValue > 0.1f;
        _colHitCount = Physics.OverlapSphereNonAlloc(hand.grabSpot.position, _supplyDetectRange, _supplyHits, _supplyMask);
        //if (_isPressed && _colHitCounts > 0 && !_isFeverTime) <-- 본래 조건문
        if (_isPressed && _colHitCount > 0)
        {
            Supply(hand);
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
    }
    private void Spray(HandData hand)
    {
        _sprayStartPos = _sprayOrigin.transform.position;
        _sprayEndPos = _sprayStartPos + (_sprayOrigin.forward * _sprayLength);
        _fireHitCount = Physics.OverlapCapsuleNonAlloc(_sprayStartPos, _sprayEndPos, _sprayRadius, _fireHits, _fireMask);
        if (!hand.normalFireFX)
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
    private void Supply(HandData hand)
    {
        if (!_rightHand.enabled && !_leftHand.enabled)
        {
            hand.modelPrefab.SetActive(true);
            hand.enabled = true;
            _sprayOrigin = hand.modelPrefab.transform.Find("SprayOrigin");
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
    public void SetAmountZero() => _currentAmount = 0;
    private void OnDrawGizmos()
    {
        DrawSprayRange(_leftHand);
        DrawSprayRange(_rightHand);
    }
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
