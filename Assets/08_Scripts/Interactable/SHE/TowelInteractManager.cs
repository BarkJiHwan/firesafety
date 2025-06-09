using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TowelInteractManager : MonoBehaviour
{
    public class TowelHandData
    {
        public Transform grabSpot;
        public InputActionProperty triggerAction;
        public GameObject modelPrefab;
        public GameObject wetPrefab;
        public bool isWet;
        public bool isEnabled = false;
    }
    [Header("양손 수건 데이터")]
    [SerializeField] private TowelHandData _leftHand;
    [SerializeField] private TowelHandData _rightHand;
    [Header("수건")]
    [SerializeField] private LayerMask _towelMask;
    [SerializeField] private LayerMask _waterMask;
    [SerializeField] private float _towelInteractWater;
    [SerializeField] private float _towelInteractRadius;
    private int _towelColHits;
    private float _triggerValue;
    private Collider[] _towelHitCols;
    private bool _gotTowel = false;
    private void Update()
    {
        CheckCols(_leftHand);
        CheckCols(_rightHand);
    }

    private void CheckCols(TowelHandData hand)
    {
        _triggerValue = hand.triggerAction.action.ReadValue<float>();
        if (!_gotTowel)
        {
            _towelColHits = Physics.OverlapSphereNonAlloc(hand.grabSpot.position, _towelInteractRadius, _towelHitCols, _towelMask);
            if (_towelColHits > 0)
            {
                TowelSupply(hand);
                _towelColHits = 0;
            }
        }
        if (_gotTowel)
        {
            _towelColHits = Physics.OverlapSphereNonAlloc(hand.grabSpot.position, _towelInteractWater, _towelHitCols, _waterMask);
        }
    }
    private void TowelSupply(TowelHandData hand)
    {
        if (!hand.isEnabled && !_gotTowel)
        {
            hand.isEnabled = true;
            _gotTowel = true;
            Instantiate(hand.modelPrefab, hand.grabSpot.position, Quaternion.identity);
        }
    }
}
