using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class FireSuppressantManager : MonoBehaviour
{
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

        public int amount = 600;
        public bool enabled = false;
        public bool isSpraying = false;
    }

    [Header("양손 소화기 데이터")]
    [SerializeField] private HandData _leftHand;
    [SerializeField] private HandData _rightHand;

    [Header("공통 설정")]
    [SerializeField] private float _sprayLength = 2.5f;
    [SerializeField] private float _sprayRadius = 1;
    [SerializeField] private int _damage = 1;
    [SerializeField] private int _decreaseAmount = 1;
    [SerializeField] private LayerMask _fireMask;
    [SerializeField] private float _refillCooldown = 3f;
    [SerializeField] private LayerMask _supplyMask;
    [SerializeField] private float _supplyDetectRange = 0.8f;
    [SerializeField] private bool _isFeverTime;
    [SerializeField] private float _supplyCooldown;
    [SerializeField] private Transform _sprayOrigin; //스프레이 발사 지점

    private readonly WaitForSeconds _checkTime = new(0.05f);
    private readonly WaitForSeconds _fireDelay = new(0.3f);

    private readonly Collider[] _fireHits = new Collider[20];
    private readonly Collider[] _supplyHits = new Collider[10];
    private readonly Dictionary<Collider, IDamageable> _cacheds = new();
    private readonly Collider[] _checkingCols = new Collider[20];
    private Vector3 _sprayStartPos;
    private Vector3 _sprayEndPos;
    private float _triggerValue;
    private bool _isPressed;
    private int _colHitCount;
    private int _fireHitCount;
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
        //if (_isPressed && _colHitCounts > 0)
        if (_colHitCount > 0)//테스트용
        {
            Supply(hand);
            _supplyCooldown = _refillCooldown;
        }
        if (_isPressed && !hand.isSpraying && hand.enabled)
        {
            hand.isSpraying = true;
            StartCoroutine(SuppressingFire(hand));
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
        while (_triggerValue > 0.1f && hand.amount > 0)
        {
            hand.initialFireFX.Play();
            yield return _fireDelay;
            hand.initialFireFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (hand.amount > 0 && !_isFeverTime)
            {
                hand.amount -= _decreaseAmount;
            }
            Spray(hand);
            yield return _checkTime;
        }
        while (_triggerValue > 0.1f && hand.amount <= 0)
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
        //이펙트 끄기
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
        yield return null;
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
        if (hand.enabled && hand.amount < 600)
        {
            hand.amount = 600;
        }
    }
    private void FeverTimeOn(HandData hand)
    {
        _damage *= 2;
        hand.amount = 100;
    }
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
