using System.Collections;
using UnityEngine;

/// <summary>
/// 대피씬 전용 소백이 - 플레이어 카메라 따라다니기 + 소백카 전환만
/// 애니메이션 없이 둥둥 떠다니기만 구현
/// </summary>
public class ExitSobaek : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("기본 위치 설정")]
    [SerializeField] private float offsetX = 1.5f;
    [SerializeField] private float offsetY = 0.5f;
    [SerializeField] private float offsetZ = 0.5f;
    [SerializeField] private bool stayOnRightSide = true;

    [Header("둥둥 떠다니기 효과")]
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float lookAtSpeed = 2f;

    [Header("이동 설정")]
    [SerializeField] private float followSpeed = 5f;
    #endregion

    #region 프로퍼티
    public static ExitSobaek Instance { get; private set; }
    public Transform Player { get => playerCamera; set => playerCamera = value; }
    public GameObject SobaekCar { get => sobaekCarObject; set => sobaekCarObject = value; }
    #endregion

    #region 변수 선언
    private Transform playerCamera;
    private GameObject sobaekCarObject;
    private Vector3 homePosition;
    private Vector3 basePosition;

    private float floatTimer = 0f;
    #endregion

    #region 유니티 라이프사이클
    /// <summary>
    /// 싱글톤 초기화
    /// </summary>
    void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// 초기 위치 설정 및 소백카 비활성화
    /// </summary>
    void Start()
    {
        SetupInitialPosition();
        SetupSobaekCar();
    }

    /// <summary>
    /// 매 프레임 이동 및 효과 업데이트
    /// </summary>
    void LateUpdate()
    {
        UpdateMovementAndEffects();
    }

    /// <summary>
    /// 싱글톤 정리
    /// </summary>
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 싱글톤 패턴 초기화
    /// </summary>
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 소백카 초기 상태 설정 (비활성화)
    /// </summary>
    private void SetupSobaekCar()
    {
        if (sobaekCarObject != null)
        {
            sobaekCarObject.SetActive(false);
        }
    }

    /// <summary>
    /// 초기 위치 설정
    /// </summary>
    private void SetupInitialPosition()
    {
        if (playerCamera != null)
        {
            SetHomePosition();
            basePosition = homePosition;
            transform.position = homePosition;
        }
    }
    #endregion

    #region 위치 및 이동
    /// <summary>
    /// 플레이어 카메라 기준 홈 포지션 계산
    /// </summary>
    private void SetHomePosition()
    {
        if (playerCamera == null)
            return;

        Vector3 rightDirection = playerCamera.right * (stayOnRightSide ? offsetX : -offsetX);
        Vector3 forwardDirection = playerCamera.forward * offsetZ;
        homePosition = playerCamera.position + rightDirection + forwardDirection + Vector3.up * offsetY;
    }

    /// <summary>
    /// 이동 및 효과 업데이트
    /// </summary>
    private void UpdateMovementAndEffects()
    {
        UpdatePosition();
        UpdateFloatingEffect();
    }

    /// <summary>
    /// 위치 업데이트 (카메라 따라가기)
    /// </summary>
    private void UpdatePosition()
    {
        if (playerCamera == null)
            return;

        // 카메라 기준으로 홈 포지션 계속 갱신
        SetHomePosition();

        // 홈 포지션으로 부드럽게 이동
        basePosition = Vector3.Slerp(basePosition, homePosition, followSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 둥둥 떠다니기 효과 및 회전 업데이트
    /// </summary>
    private void UpdateFloatingEffect()
    {
        floatTimer += Time.deltaTime * floatSpeed;
        float floatY = Mathf.Sin(floatTimer) * floatAmplitude;
        transform.position = basePosition + Vector3.up * floatY;

        UpdateLookDirection();
    }

    /// <summary>
    /// 플레이어 카메라 바라보기
    /// </summary>
    private void UpdateLookDirection()
    {
        if (playerCamera == null)
            return;

        // 플레이어 카메라를 바라보기
        Vector3 targetDirection = (playerCamera.position - transform.position).normalized;
        targetDirection.y = 0;

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
        }
    }
    #endregion

    #region 소백이/소백카 관리
    /// <summary>
    /// 소백이 비활성화 및 소백카 활성화
    /// </summary>
    public void ActivateSobaekCar()
    {
        if (sobaekCarObject != null)
        {
            sobaekCarObject.SetActive(true);
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("SobaekCar 오브젝트가 설정되지 않았습니다.");
        }
    }
    #endregion
}
