using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class Phase2InteractManager : MonoBehaviour
{
    [System.Serializable]
    public class TowelHandData
    {
        public InputActionProperty triggerAction;
        public GameObject towelModelPrefab;
        public GameObject wetPrefab;
        public bool isWet;
        public bool isEnabled = false;//수건을 가지고 있는가
        public XRRayInteractor interactor;
        public EHandType handType;
        public ActionBasedController xrController;
    }
    private Dictionary<EHandType, TowelHandData> _handDatasDict = new();
    public bool IsWear
    {
        get; private set;
    }
    [Header("양손 수건 데이터")]
    [SerializeField] private TowelHandData _leftHand;
    [SerializeField] private TowelHandData _rightHand;
    [Header("수건")]
    [SerializeField] private bool _gotTowel = false;
    public bool gotWet = false;
    private void Start()
    {
        _handDatasDict[EHandType.LeftHand] = _leftHand;
        _handDatasDict[EHandType.RightHand] = _rightHand;
        //수건을 Dictionary에 등록해두고, 밑에 GetHand함수로 수건 값을 가져올 수 있도록 함.
    }
    private TowelHandData GetHand(EHandType type)
    {
        if (_handDatasDict.TryGetValue(type, out var hand))
        {
            return hand;
        }
        return null;
    }
    //본래 수건을 정말 입에 대려고 했으나, 캐릭터 기준으로는 어림 없었고 카메라 기준도 애매모호하다고 판단해 폐기.
    //하지만 다음으로 진행했을 때 수건을 꺼주는 용도로 사용
    public void CheckingTowelCol()
    {
        IsWear = true;
        if (_leftHand.isEnabled)
        {
            _leftHand.wetPrefab.SetActive(false);
        }
        if (_rightHand.isEnabled)
        {
            _rightHand.wetPrefab.SetActive(false);
        }
    }
    /// <summary>
    /// 수건을 활성화해주는 함수. XR Simple Interactor에서 EHandType을 넘겨주면 된다.
    /// </summary>
    /// <param name="type"></param>
    public void TowelSupply(EHandType type)
    {
        var hand = GetHand(type);
        if (!hand.isEnabled && !_gotTowel)
        {
            hand.isEnabled = true;
            _gotTowel = true;
            hand.towelModelPrefab.SetActive(true);
        }
    }
    /// <summary>
    /// 수건을 가진 손이 상호작용할 경우 동작하는 함수. EHandType을 넘겨주면 된다.
    /// </summary>
    /// <param name="type"></param>
    public void WettingTowel(EHandType type)
    {
        var hand = GetHand(type);
        if (!gotWet)
        {
            hand.isWet = true;
            hand.towelModelPrefab.SetActive(false);
            hand.wetPrefab.SetActive(true);
            gotWet = true;
            CheckingTowelCol();
        }
    }
}
