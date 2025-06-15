using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class Phase2InteractManager : MonoBehaviour
{
    public class TowelHandData
    {
        public Transform grabSpot;
        public InputActionProperty triggerAction;
        public GameObject modelPrefab;
        public GameObject wetPrefab;
        public bool isWet;
        public bool isEnabled = false;
        public XRRayInteractor interactor;
    }
    [SerializeField] private DaTaewoori _daTaewoori;
    [SerializeField] private GameObject _checkingWearing; //수건 입에 가져다 댄 건가요?
    [SerializeField] private bool _isWear;//가져다 댄 거 맞지요?
    [Header("양손 수건 데이터")]
    [SerializeField] private TowelHandData _leftHand;
    [SerializeField] private TowelHandData _rightHand;
    [Header("수건")]
    [SerializeField] private LayerMask _towelMask;
    [SerializeField] private LayerMask _waterMask;
    [SerializeField] private float _towelInteractWater;
    [SerializeField] private float _towelInteractRadius;
    private int _towelHitNums;
    private float _triggerValue;
    private Collider[] _towelHitCols;
    private Collider[] _tapHitCols;
    private int _tapHitNums;
    private bool _gotTowel = false;
    private bool _gotWet = false;
    [Header("수도")]
    [SerializeField] private float _tapInteractRadius;
    [SerializeField] private LayerMask _tapMask;
    [SerializeField, Tooltip("배치 필쑤!!")] private TapWater _tapWater;
    [Header("물대포")]
    [SerializeField] private int _damage = 1;
    [SerializeField] private GameObject _waterShooterPrefab;
    [SerializeField] private float _fireDelay = 0.5f;
    [SerializeField] private LayerMask _weaponLayer;
    private RaycastHit hit;
    private void Awake()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentPhase != GamePhase.LeaveDangerArea)
        {
            enabled = false;
        }
    }
    private void Update()
    {
        CheckCols(_leftHand);
        CheckCols(_rightHand);
    }

    private void CheckCols(TowelHandData hand)
    {
        _triggerValue = hand.triggerAction.action.ReadValue<float>();
        if (hand.interactor.TryGetCurrent3DRaycastHit(out hit) && !_gotTowel)//수건 가져오기
        {
            if (FireSuppressantManager.IsInLayerMask(hit.collider.gameObject, _towelMask))
            {
                TowelSupply(hand);
            }
        }
        if (_gotTowel && hand.interactor.TryGetCurrent3DRaycastHit(out hit))//물에 적시기
        {
            if (FireSuppressantManager.IsInLayerMask(hit.collider.gameObject, _waterMask))
            {
                WettingTowel(hand);
            }
        }
        _tapHitNums = Physics.OverlapSphereNonAlloc(hand.grabSpot.position, _tapInteractRadius, _tapHitCols, _tapMask);
        if (_tapHitNums > 0 && _triggerValue > 0.1f)//수도 상호작용
        {
            _tapWater.InteractTapWater();
        }
    }
    private void TowelSupply(TowelHandData hand)
    {
        if (!hand.isEnabled && !_gotTowel)
        {
            hand.isEnabled = true;
            _gotTowel = true;
            hand.modelPrefab = Instantiate(hand.modelPrefab, hand.grabSpot.position, Quaternion.identity);
        }
    }
    private void WettingTowel(TowelHandData hand)
    {
        if (!_gotWet)
        {
            hand.isWet = true;
            hand.modelPrefab.SetActive(false);
            hand.wetPrefab = Instantiate(hand.wetPrefab, hand.grabSpot.position, Quaternion.identity);
            _gotWet = true;
        }
    }
}
