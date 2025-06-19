using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class SupplyManager : MonoBehaviourPunCallbacks
{
    public static SupplyManager Instance { get; private set; }

    [SerializeField] private Dictionary<EHandType, HandData> _hands = new();
    [SerializeField] private Dictionary<EHandType, HandData> _handsTuto = new();
    public FireSuppressantManager suppressantManager;
    public TutorialSuppressor tutorialSuppressor;
    public PhotonView pView;

    private void Awake()
    {
        if (!pView.IsMine)
        { Destroy(this); return; }
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void RegisterHand(EHandType type, HandData data, bool isTutorial)
    {
        if (!_hands.ContainsKey(type) && !isTutorial)
        {
            _hands.Add(type, data);
            Debug.Log($"손 등록 - 본게임 {type}");
        }
        if (!_handsTuto.ContainsKey(type) && isTutorial)
        {
            _handsTuto.Add(type, data);
            Debug.Log($"손 등록 - 튜토리얼 {type}");
        }
    }
    public void Supply(EHandType type)
    {
        if (GameManager.Instance.IsGameStart)
        {
            if (!_hands.ContainsKey(type))
            {
                Debug.LogWarning($"등록되지 않은 손: {type}");
                return;
            }
            suppressantManager.Supply(type);
            Debug.Log("본게임 보급을 불렀다");
        }
        else if (!GameManager.Instance.IsGameStart)
        {
            if (!_handsTuto.ContainsKey(type))
            {
                Debug.LogWarning($"등록되지 않은 손: {type}");
                return;
            }
            tutorialSuppressor.Supply(type);
            Debug.Log("튜토리얼 보급을 불렀다");
        }
    }
}
