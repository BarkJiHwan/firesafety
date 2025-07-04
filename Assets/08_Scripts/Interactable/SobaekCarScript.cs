using System;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.XR.Interaction.Toolkit;

public class SobaekCarScript : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("기본 설정")]
    public GameObject seatPosition;
    public GameObject player;
    #endregion

    #region 내부 변수
    private SplineContainer splineContainer; // CHM 변경 플레이어 스포너에서 받아옴 파인드 오브젝트 관련 지움
    private ExitSupplyManager exitMgr;
    private PlayerSpawner _playerSpawner;
    private ExitDialogue _exitDialogue;
    #endregion

    #region 컴포넌트 참조
    private XRSimpleInteractable simpleInteractable;
    private SplineAnimate splineAnimate;
    #endregion

    #region 유니티 라이프사이클
    private void Awake()
    {
        InitializeComponents();
        SetupInteraction();

        exitMgr = FindObjectOfType<ExitSupplyManager>();
        _playerSpawner = FindObjectOfType<PlayerSpawner>();
        SetSplineContainer(_playerSpawner.CarTrack);
    }
    #endregion

    #region 초기화
    private void InitializeComponents()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();
        splineAnimate = GetComponent<SplineAnimate>();
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
            // 소화전 빛나는거 추가
            exitMgr.SetFireAlarmMat(true);
            _exitDialogue.OnBeforeStartShootingTrack();
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

        if (splineContainer == null)
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
        _exitDialogue = player.GetComponent<PlayerComponents>().exitDialogue;
    }

    public void SetSplineContainer(SplineContainer container)
    {
        splineContainer = container;
        if (splineAnimate != null)
        {
            splineAnimate.Container = splineContainer;
        }
    }
    #endregion

    public void OnTriggerEnter(Collider other)
    {
        // 4층 시작점일때 퀴즈 켜기
        if (other.gameObject.name.Equals("Floor4 WayPoints.Start"))
        {
            _exitDialogue.ShowQuizUI();
        }

        // 4층 종료지점일때 퀴즈 끄기
        if (other.gameObject.name.Equals("Floor4 WayPoints.End") && _exitDialogue.quizUI.gameObject.activeSelf)
        {
            _exitDialogue.OnSelectRightAnswer();
        }

        if (other.gameObject.name.Equals("Floor2 WayPoints.Start"))
        {
            _exitDialogue.OnStartSmokePlace();
        }

        if (other.gameObject.name.Equals("Floor2 WayPoints.End"))
        {
            _exitDialogue.SendSmokeScore();
        }

        if (other.gameObject.name.Equals("Floor1 WayPoints.End"))
        {
            _exitDialogue.SendDaTaewooriScore();
        }
    }
}
