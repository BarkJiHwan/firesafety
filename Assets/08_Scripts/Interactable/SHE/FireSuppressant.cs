using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class FireSuppressant : MonoBehaviour
{
    [Header("스프레이 설정")]
    [SerializeField, Tooltip("소화기 HP")] private int _amount = 600;
    [SerializeField, Tooltip("틱당 데미지")] private float _damage= 0.5f;
    [SerializeField, Tooltip("틱당 감소량")] private int _decreaseAmount= 1;
    private readonly WaitForSeconds _checkTime = new(0.05f);
    private readonly WaitForSeconds _fireDelay = new(0.3f);
    [SerializeField, Tooltip("소화기를 들고 있느뇨")] private bool _enabled;
    [SerializeField, Tooltip("피버 타임")] private bool _feverTime;
    [SerializeField, Tooltip("대피 페이즈")] private bool _runningPhase = false;
    [SerializeField, Tooltip("상호작용 키 할당")] private InputActionProperty _actionProperty;
    [SerializeField, Tooltip("소화기 보급 마스크")] private LayerMask _supplyMask;
    [SerializeField, Tooltip("소화기 분사 길이")] private float _range;
    private float _triggerValue;//눌렀는지 검사용
    [SerializeField, Tooltip("소화기 모델 프리팹")] private GameObject _modelPrefab;
    private GameObject _originalController; //원래 있던 걸 담아 놓을 곳
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
    private Vector3 _startPos;//스프레이 시작점
    private Vector3 _endPos;//스프레이 끝나는 점
    public bool Enabled
    {
        get { return _enabled; }
        set { _enabled = value; }
    }

    private void Update()
    {
        if (!_runningPhase)
        {
            _triggerValue = _actionProperty.action.ReadValue<float>();
            if (_actionProperty.action.WasPressedThisFrame() && _enabled)
            {
                StartCoroutine(SuppressingFire());
            }
            else if(_triggerValue < 0.1f)
            {
                StopAllCoroutines();
            }

            if(!_enabled && _triggerValue > 0.1f)
            {
                if (Physics.OverlapSphere(transform.position, 4, _supplyMask) != null)
                {
                    _enabled = true;
                    //컨트롤러 모델 변경
                    
                }
            }
        }
        
    }
    public void NowOnRunningPhase()
    {
        _runningPhase = true;
        //컨트롤러 소화기에서 변경해주기
    }
    public void FeverTimeOn()
    {
        _damage *= 2;
        _amount = 10000000;
        //내구도 UI 변경, 이미지로 무한이 뜨거나 할 것
        _infinityImage.SetActive(true);
        _suppressorAmountUI.text = "";
    }

    private void Spray()
    {
        _startPos = _sprayOrigin.transform.position;
        _endPos = _startPos + _sprayOrigin.forward * _sprayLength;

        Collider[] hits = Physics.OverlapCapsule(_startPos, _endPos, _sprayRadius, _fireMask);
        _normalFireFX.Play();
        foreach (var hit in hits)
        {
            if(!_cacheds.TryGetValue(hit, out IDamageable hittable))
            {
                hittable = hit.GetComponent<IDamageable>();
                if(hittable != null)
                {
                    _cacheds[hit] = hittable;
                }
            }

            hittable?.TakeDamage(_damage);
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
            if(_amount > 0 && !_feverTime)
            {
                _amount -= _decreaseAmount;
                UpdateAmountUI();
            }
           Spray();
           
           yield return _checkTime;
       }
       while(_triggerValue > 0.1f && _amount <= 0)
        {
            if (_normalFireFX.isPlaying)
            {
                _normalFireFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            _zeroAmountFireFX.Play();
        }
       if(_normalFireFX.isPlaying || _zeroAmountFireFX.isPlaying)
        {
            _normalFireFX.Stop();
            _zeroAmountFireFX.Stop();
        }

        _cacheds.Clear();
        yield return null;
    }

}
