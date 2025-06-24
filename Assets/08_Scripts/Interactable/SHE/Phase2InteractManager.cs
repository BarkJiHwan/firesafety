using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
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
        public ParticleSystem chargingEffect;
        public ParticleSystem shootingFlash;
        public bool isWet;
        public bool isEnabled = false;//수건을 가지고 있는가
        public bool activatedWeapon = false;
        public XRRayInteractor interactor;
        public EHandType handType;
        public ActionBasedController xrController;
    }
    private Dictionary<EHandType, TowelHandData> _handDatasDict = new();
    public bool IsWear
    {
        get; private set;
    }
    [Header("Common")]
    [SerializeField] private GameObject _weaponPrefab;
    [Header("양손 수건 데이터")]
    [SerializeField] private TowelHandData _leftHand;
    [SerializeField] private TowelHandData _rightHand;
    [Header("수건")]
    private float _triggerValue;
    [SerializeField] private bool _gotTowel = false;
    public bool gotWet = false;
    [Header("물대포")]//일단 다른 것부터 해보자고 ㅇ
    [SerializeField] private bool _nowCharging;
    [SerializeField] private int _count;
    [SerializeField] private float _bombSpeed = 3f;
    public bool EncounterBoss
    {
        get; set;
    }
    private void Start()
    {
        _handDatasDict[EHandType.LeftHand] = _leftHand;
        _handDatasDict[EHandType.RightHand] = _rightHand;
    }
    private TowelHandData GetHand(EHandType type)
    {
        if (_handDatasDict.TryGetValue(type, out var hand))
        {
            return hand;
        }
        return null;
    }
    private void Update()
    {
        if (EncounterBoss)
        {
            ShootingWaterBomb(_leftHand);
            ShootingWaterBomb(_rightHand);
        }
    }
    public void CheckingTowelCol()//가져다 댄 거 맞음??
    {
        IsWear = true;
        if (_leftHand.isEnabled)
        {
            _leftHand.towelModelPrefab.SetActive(false);
        }
        if (_rightHand.isEnabled)
        {
            _rightHand.towelModelPrefab.SetActive(false);
        }
    }
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
    public void GrabWeapon(EHandType type)
    {
        var hand = GetHand(type);
        if (IsWear && !hand.activatedWeapon)
        {
            hand.activatedWeapon = true;
            _weaponPrefab.SetActive(true);
            if (hand.xrController.modelPrefab == null)
            {
                Debug.LogWarning("없다 모델이");
            }
            //foreach (Transform child in hand.xrController.modelParent)
            //{
            //    Destroy(child.gameObject);
            //}
            
            var newModel = Instantiate(_weaponPrefab, hand.xrController.modelParent);
            newModel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            var particle = newModel.GetComponentInChildren<Phase2_WeaponFX>();
            hand.chargingEffect = particle.chargeFX;
            hand.shootingFlash = particle.shootingFX;
            hand.xrController.modelPrefab = _weaponPrefab.transform;

            //var particles = newModel.GetComponents<ParticleSystem>();
            //foreach (var particle in particles)
            //{
            //    if (particle.name.ToLower().Contains("Charging"))
            //    {
            //        hand.chargingEffect = particle;
            //    }
            //    else if (particle.name.ToLower().Contains("Shooting"))
            //    {
            //        hand.shootingFlash = particle;
            //    }
            //}
        }
    }
    private void ShootingWaterBomb(TowelHandData hand)
    {
        if (!hand.activatedWeapon)
        {
            return;
        }
        if (hand.triggerAction.action.WasPressedThisFrame() && !_nowCharging)
        {
            _nowCharging = true;
            StartCoroutine(ChargeAndShoot(hand));
        }

    }
    private IEnumerator ChargeAndShoot(TowelHandData hand)
    {
        if (_count != 0)
        {
            _count = 0;
        }
        if (!hand.chargingEffect.isPlaying)
        {
            hand.chargingEffect.Play();
        }
        while(_triggerValue > 0)
        {
            _count++;
            yield return new WaitForSeconds(0.1f);
        }
        if (hand.chargingEffect.isPlaying)
        {
            hand.chargingEffect.Stop();
        }
        if (_count >= 2)
        {
            ShootingBigBomb(hand);
        }
        else
        {
            ShootingSmallBomb(hand);
        }
    }
    private void ShootingBigBomb(TowelHandData hand)
    {
        var rayOrigin = hand.interactor.transform;
        var bomb = Phase2_BombPoolManager.Instance.GetBigBomb();
        bomb.transform.SetPositionAndRotation(rayOrigin.position, Quaternion.LookRotation(rayOrigin.forward));
        var rb = bomb.GetComponent<Rigidbody>();
        rb.velocity = rayOrigin.forward * _bombSpeed;
        _count = 0;
    }
    private void ShootingSmallBomb(TowelHandData hand)
    {
        var rayOrigin = hand.interactor.transform;
        var bomb = Phase2_BombPoolManager.Instance.GetSmallBomb();
        bomb.transform.SetPositionAndRotation(rayOrigin.position, Quaternion.LookRotation(rayOrigin.forward));
        var rb = bomb.GetComponent<Rigidbody>();
        rb.velocity = rayOrigin.forward * _bombSpeed;
        _count = 0;
    }
}
