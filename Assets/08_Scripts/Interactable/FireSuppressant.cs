using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class FireSuppressant : MonoBehaviour
{
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
    private float triggerValue;//눌렀는지 검사용
    [SerializeField, Tooltip("소화기 모델 프리팹")] GameObject _modelPrefab;
    private GameObject _originalController; //원래 있던 걸 담아 놓을 곳
    public bool Enabled
    {
        get { return _enabled; }
        set { _enabled = value; }
    }

    private void Update()
    {
        if (!_runningPhase)
        {
            triggerValue = _actionProperty.action.ReadValue<float>();
            if (_actionProperty.action.WasPressedThisFrame() && _enabled)
            {
                StartCoroutine(SuppressingFire());
            }
            else if(triggerValue < 0.1f)
            {
                StopAllCoroutines();
            }

            if(!_enabled && triggerValue > 0.1f)
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
        _amount *= 50;
    }

    IEnumerator SuppressingFire()
    {
        while(triggerValue > 0.1f)
        {
            yield return _fireDelay;
            _amount -= _decreaseAmount;
            //대충 쏘고 맞으면 딜 넣는다는 내용
            yield return _checkTime;
        }
        yield return null;
    }

}
