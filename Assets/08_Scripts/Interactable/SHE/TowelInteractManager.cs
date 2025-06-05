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
    }
    [Header("양손 수건 데이터")]
    [SerializeField] private TowelHandData _leftHand;
    [SerializeField] private TowelHandData _righjtHand;
    [Header("수건")]
    [SerializeField] private LayerMask _towelMask;
    [SerializeField] private float _towelInteractLenght;
    [SerializeField] private float _towelInteractRadius;
    private int _towelColHits;
}
