using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class SupplyManager : MonoBehaviour
{
    public static SupplyManager Instance { get; private set; }

    private Dictionary<EHandType, HandData> _hands = new();
    public FireSuppressantManager suppressantManager;
    public TutorialSuppressor tutorialSuppressor;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // PhotonView가 존재하면 내 것만 유지
        var pv = GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
        {
            Destroy(this);
            return;
        }
    }
    public void RegisterHand(EHandType type, HandData data)
    {
        if (!_hands.ContainsKey(type))
        {
            _hands.Add(type, data);
        }
    }
    public void Supply(EHandType type)
    {
        if (!_hands.ContainsKey(type))
        {
            Debug.LogWarning($"등록되지 않은 손: {type}");
            return;
        }
        Debug.Log($"[SupplyManager] {type} 손에 보급");

        if (GameManager.Instance != null && GameManager.Instance.IsGameStart)
        {
            suppressantManager?.Supply(type);
        }
        else
        {
            tutorialSuppressor?.Supply(type);
        }
    }
}
