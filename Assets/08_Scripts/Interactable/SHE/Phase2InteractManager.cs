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
        public EHandType handType;
    }
    [SerializeField] private DaTaewoori _daTaewoori;
    public bool IsWear
    {
        get; private set;
    }
    [Header("양손 수건 데이터")]
    [SerializeField] private TowelHandData _leftHand;
    [SerializeField] private TowelHandData _rightHand;
    [Header("수건")]
    [SerializeField] private LayerMask _towelMask;
    [SerializeField] private LayerMask _waterMask;
    [SerializeField] private float _towelInteractWater;
    [SerializeField] private float _towelInteractRadius;
    private float _triggerValue;
    private bool _gotTowel = false;
    private bool _gotWet = false;
    [Header("수도")]
    [SerializeField] private float _tapInteractRadius;
    [SerializeField] private LayerMask _tapMask;
    [SerializeField, Tooltip("배치 필쑤!!")] private TapWater _tapWater;
    [SerializeField] private bool _tapEnable = false;
    [Header("물대포")]
    [SerializeField] private int _damage = 1;
    [SerializeField] private GameObject _waterShooterPrefab;
    [SerializeField] private float _fireDelay = 0.5f;
    [SerializeField] private LayerMask _weaponLayer;
    private RaycastHit _hit;
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
    public void CheckingTowelCol()//가져다 댄 거 맞음??
    {
        IsWear = true;
        if (_leftHand.isEnabled)
        {
            _leftHand.modelPrefab.SetActive(false);
        }
        if (_rightHand.isEnabled)
        {
            _rightHand.modelPrefab.SetActive(false);
        }
    }
    private void CheckCols(TowelHandData hand)
    {
        _triggerValue = hand.triggerAction.action.ReadValue<float>();

    }
    private void TowelSupply(TowelHandData hand)
    {
        if (!hand.isEnabled && !_gotTowel)
        {
            hand.isEnabled = true;
            _gotTowel = true;
            hand.modelPrefab = Instantiate(hand.modelPrefab, hand.grabSpot.position, Quaternion.identity);
            //생성 -> 활성화 비활성화로 ㅇ
        }
    }
    private void WettingTowel(TowelHandData hand)
    {
        if (!_gotWet)
        {
            hand.isWet = true;
            hand.modelPrefab.SetActive(false);
            hand.wetPrefab = Instantiate(hand.wetPrefab, hand.grabSpot.position, Quaternion.identity);
            //생성 -> 활성화 비활성화로 ㅇ
            _gotWet = true;
        }
    }
}
