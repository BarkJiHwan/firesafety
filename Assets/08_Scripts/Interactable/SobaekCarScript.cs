using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.XR.Interaction.Toolkit;

public enum CarSpeed
{
    Stop,
    GearOne,
    GearTwo,
    GearThree
}

public class SobaekCarScript : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("기본 설정")]
    public GameObject seatPosition;
    public GameObject player;

    [Header("스플라인 설정")]
    public SplineContainer splineContainer;
    #endregion

    #region 컴포넌트 참조
    private XRSimpleInteractable simpleInteractable;
    private SplineAnimate splineAnimate;
    #endregion

    #region 유니티 라이프사이클
    private void Awake()
    {
        InitializeComponents();
        SetupSplineContainer();
        SetupInteraction();
    }
    #endregion

    #region 초기화
    private void InitializeComponents()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();
        splineAnimate = GetComponent<SplineAnimate>();

        // SplineAnimate가 없으면 자동 추가
        if (splineAnimate == null)
        {
            splineAnimate = gameObject.AddComponent<SplineAnimate>();
        }
    }

    private void SetupSplineContainer()
    {
        if (splineAnimate == null)
            return;

        // 이미 설정되어 있으면 패스
        if (splineAnimate.Container != null)
            return;

        // 우선순위: 인스펙터 설정 → 자식 → 씬 전체
        SplineContainer container = GetSplineContainer();
        if (container != null)
        {
            splineContainer = container;
            splineAnimate.Container = container;
        }
    }

    private SplineContainer GetSplineContainer()
    {
        // 1. 인스펙터에서 설정된 경우
        if (splineContainer != null)
            return splineContainer;

        // 2. 자식 오브젝트에서 찾기
        SplineContainer childContainer = GetComponentInChildren<SplineContainer>();
        if (childContainer != null)
            return childContainer;

        // 3. 씬에서 찾기
        return FindObjectOfType<SplineContainer>();
    }

    private void SetupInteraction()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.selectEntered.AddListener(OnEnteredCar);
        }
    }
    #endregion

    #region 상호작용
    private void OnEnteredCar(SelectEnterEventArgs args)
    {
        if (TrySetPlayerPosition())
        {
            DisableInteraction();
            
        }
    }

    private bool TrySetPlayerPosition()
    {
        if (player == null || seatPosition == null)
            return false;

        player.transform.position = seatPosition.transform.position;
        player.transform.rotation = seatPosition.transform.rotation;
        player.transform.parent = gameObject.transform;

        return true;
    }

    private void DisableInteraction()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.selectEntered.RemoveListener(OnEnteredCar);
            simpleInteractable.enabled = false;
        }
    }
    #endregion

    #region 소백카 제어
    public void StartTrack()
    {
        if (!ValidateComponents())
            return;
        splineAnimate.Play();
    }

    private bool ValidateComponents()
    {
        if (splineAnimate == null)
        {
            Debug.LogWarning("SplineAnimate 컴포넌트가 없습니다!");
            return false;
        }

        if (splineAnimate.Container == null)
        {
            Debug.LogWarning("SplineContainer가 설정되지 않았습니다!");
            return false;
        }

        return true;
    }
    #endregion

    #region 유틸리티
    public void SetPlayer(GameObject newPlayer)
    {
        player = newPlayer;
    }

    public void SetSplineContainer(SplineContainer container)
    {
        splineContainer = container;
        if (splineAnimate != null)
        {
            splineAnimate.Container = container;
        }
    }
    #endregion
}
