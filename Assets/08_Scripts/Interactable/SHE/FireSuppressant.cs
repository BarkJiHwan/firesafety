using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;

public class FireSuppressant : MonoBehaviour
{
    [Header("스프레이 설정")]
    [SerializeField, Tooltip("소화기 HP")] private int _amount = 600;
    [SerializeField, Tooltip("틱당 데미지")] private float _damage = 0.5f;
    [SerializeField, Tooltip("틱당 감소량")] private int _decreaseAmount = 1;
    private readonly WaitForSeconds _checkTime = new(0.05f);
    private readonly WaitForSeconds _fireDelay = new(0.3f);
    [SerializeField, Tooltip("지금 쏘고 있나요")] private bool _fireEnabled;
    [Tooltip("소화기를 들고 있느뇨")] public bool Enabled { get; set; }
    [SerializeField, Tooltip("피버 타임")] private bool _feverTime;
    [SerializeField, Tooltip("대피 페이즈")] private bool _runningPhase = false;
    [SerializeField, Tooltip("상호작용 키 할당")] private InputActionProperty _actionProperty;
    [SerializeField, Tooltip("소화기 보급 마스크")] private LayerMask _supplyMask;
    [SerializeField, Tooltip("소화기 분사 길이")] private float _range;
    private float _triggerValue;//눌렀는지 검사용
    [SerializeField, Tooltip("소화기 모델 프리팹")] private GameObject _modelPrefab;
    //private GameObject _originalController; //원래 있던 걸 담아 놓을 곳으로 설계했지만 팔이 잡는다네요
    [SerializeField] private Dictionary<Collider, IDamageable> _cacheds = new();
    [SerializeField, Tooltip("스프레이 발사 시작 지점")] private Transform _sprayOrigin;
    [SerializeField, Tooltip("스프레이 길이")] private float _sprayLength;
    [SerializeField, Tooltip("스프레이 넓이")] private int _sprayRadius;
    [SerializeField, Tooltip("불 레이어")] private LayerMask _fireMask;
    [SerializeField, Tooltip("스프레이 일반 쏘기")] private ParticleSystem _normalFireFX;
    [SerializeField, Tooltip("내구도 0일 때")] private ParticleSystem _zeroAmountFireFX;
    [SerializeField, Tooltip("처음 쏠 때 허접한 FX")] private ParticleSystem _initialFireFX;
    [SerializeField, Tooltip("잔여량 UI Text")] private TextMeshPro _suppressorAmountUI;
    [SerializeField, Tooltip("무한 표시 이미지가 있는 오브젝트")] private GameObject _infinityImage;
    private Transform _grabSpot;//소화기를 실제로 들고 있는 놈, 얘 끄면 소화기도 따운
    private readonly Collider[] _checkingCols = new Collider[20];
    private readonly Collider[] _checkingSupplyCols = new Collider[20];
    private int _colHitCounts;
    private Vector3 _startPos;//스프레이 시작점
    private Vector3 _endPos;//스프레이 끝나는 점
    private bool _isPressed;//눌림?
    private bool _wasPressedLateFrame;//전프레임 트리거 값
    [SerializeField] private float _detactingRange; //소화기 리필 지점 인식 범위
    private int _supplyColHitCount;
    private float _supplyCooldown;//시간초 재야지
    [SerializeField] private float _refillCooldown;//일정 시간 후에 가져갈 수 있도록
    [SerializeField] private bool _inSupplySpot;
    private void Update()
    {
        //게임 매니저에서 대피 페이즈인지 검사 후 조치를 취하자
        //대피 페이즈에서는 소화기 비활성화 및 문 상호작용이 가능하게 해야한다.


        if (!_runningPhase)
        {
            _triggerValue = _actionProperty.action.ReadValue<float>();
            _isPressed = _triggerValue > 0.1f;
            if (_supplyCooldown > 0)
            {
                _supplyCooldown -= Time.deltaTime;
            }
            if (_isPressed && _wasPressedLateFrame && !_feverTime)
            {
                _supplyColHitCount = Physics.OverlapSphereNonAlloc(transform.position, _detactingRange, _checkingSupplyCols, _supplyMask);
                if (_supplyColHitCount > 0 && 0 > _supplyCooldown)
                {
                    SupplySuppressor(gameObject.transform);
                    //UI도 해주세요~
                }

            }
        }
        _wasPressedLateFrame = _isPressed;
        //밑은 테스트용 코드
    }

    

    private void FeverTimeOn()
    {
        _damage *= 2;
        _amount = 10000000;
        //내구도 UI 변경, 이미지로 무한이 뜨거나 할 것
        _infinityImage.SetActive(true);
        _suppressorAmountUI.text = "";
    }

    private void SupplySuppressor(Transform player)
    {
        if (!_feverTime)
        {
            if (!Enabled)
            {
                _grabSpot = player.transform.Find("GrabSpot");
                Instantiate(_modelPrefab, _grabSpot.position, _grabSpot.rotation, _grabSpot);
                Enabled = true;
            }
            _amount = 600;
            UpdateAmountUI();
        }
        else
        {
            return;
        }
    }

    private void Spray()
    {
        _startPos = _sprayOrigin.transform.position;
        _endPos = _startPos + (_sprayOrigin.forward * _sprayLength);

        _colHitCounts = Physics.OverlapCapsuleNonAlloc(_startPos, _endPos, _sprayRadius, _checkingCols, _fireMask);
        if (!_normalFireFX.isPlaying)
        {
            _normalFireFX.Play();
        }
        for (int i = 0; i < _colHitCounts; i++)
        {
            var hit = _checkingCols[i];
            if (!_cacheds.TryGetValue(hit, out var cached))
            {
                cached = GetComponent<IDamageable>();
                if (cached != null)
                {
                    _cacheds[hit] = cached;
                }
            }
            cached?.TakeDamage(_damage);
        }
    }

    private void UpdateAmountUI()
    {
        if (_amount > 0)
        {
            _suppressorAmountUI.text = (_amount / 6).ToString();
        }
    }

    private IEnumerator SuppressingFire()
    {
        while (_triggerValue > 0.1f && _amount > 0)
        {
            _initialFireFX.Play();
            yield return _fireDelay;
            _initialFireFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_amount > 0 && !_feverTime)
            {
                _amount -= _decreaseAmount;
                UpdateAmountUI();
            }
            Spray();
            yield return _checkTime;
        }
        while (_triggerValue > 0.1f && _amount <= 0)
        {
            if (_normalFireFX.isPlaying)
            {
                _normalFireFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (!_zeroAmountFireFX.isPlaying)
            {
                _zeroAmountFireFX.Play();
            }
        }
        //이펙트 끄기
        if (_normalFireFX.isPlaying)
        {
            _normalFireFX.Stop();
        }
        if (_zeroAmountFireFX.isPlaying)
        {
            _zeroAmountFireFX.Stop();
        }
        if (_initialFireFX.isPlaying)
        {
            _initialFireFX.Stop();
        }
        _cacheds.Clear();
        yield return null;
    }
    private void OnDrawGizmos()
    {
        //스프레이 범위
        if (_sprayOrigin == null)
        {
            return;
        }

        Vector3 start = _sprayOrigin.position;
        Vector3 end = start + (_sprayOrigin.forward * _sprayLength);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(start, _sprayRadius);
        Gizmos.DrawWireSphere(end, _sprayRadius);
        Gizmos.DrawLine(start, end);
        //트리거 인식 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detactingRange);
    }
}
