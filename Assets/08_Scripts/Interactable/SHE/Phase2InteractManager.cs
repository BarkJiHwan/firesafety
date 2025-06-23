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
    [Header("Common")]
    [SerializeField] private LayerMask _interactMask;
    [Header("양손 수건 데이터")]
    [SerializeField] private TowelHandData _leftHand;
    [SerializeField] private TowelHandData _rightHand;
    [Header("수건")]
    private float _triggerValue;
    [SerializeField] private bool _gotTowel = false;
    [SerializeField] private bool _gotWet = false;
    [Header("수도")]
    [SerializeField] private float _tapInteractRadius;
    [SerializeField] private bool _tapEnable = false;
    [Header("물대포")]//일단 다른 것부터 해보자고 ㅇ
    [SerializeField] private int _damage = 1;
    [SerializeField] private GameObject _waterShooterPrefab;
    [SerializeField] private float _fireDelay = 0.5f;
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
