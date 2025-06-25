using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Splines;

/// <summary>
/// 텔레포트 시스템 - 페이드 효과와 함께 플레이어 위치 이동
/// </summary>
public class Teleport : MonoBehaviour
{
    [Header("텔레포트 설정")]
    [SerializeField] private Transform teleportDestination; // 이동할 위치
    [SerializeField] private LayerMask playerLayer = -1; // 플레이어 레이어

    [Header("페이드 효과")]
    [SerializeField] private float fadeOutDuration = 1f; // 페이드 아웃 시간

    private CustomTunnelingVignette vignetteController;
    private SplineAnimate sobaekCarSpline;

    [SerializeField] private PlayableDirector _playerbleDir;

    /// <summary>
    /// 초기화 및 컴포넌트 찾기 시작
    /// </summary>
    void Start()
    {
        // 잠시 기다렸다가 찾기
        StartCoroutine(FindComponentsDelayed());
    }

    /// <summary>
    /// PlayerSpawner 초기화 대기 후 컴포넌트 찾기
    /// </summary>
    IEnumerator FindComponentsDelayed()
    {
        yield return new WaitForSeconds(0.1f); // PlayerSpawner 실행 기다리기

        vignetteController = PlayerSpawner.GetPlayerVignette();
        sobaekCarSpline = PlayerSpawner.GetSobaekCarSpline();

        if (vignetteController == null)
        {
            Debug.LogWarning("VignetteController를 찾을 수 없습니다!");
        }
        if (sobaekCarSpline == null)
        {
            Debug.LogWarning("SobaekCarSpline을 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 플레이어 트리거 진입 시 텔레포트 시작
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 레이어 체크
        if (!IsPlayerLayer(other.gameObject))
            return;

        // 텔레포트 대상 위치가 설정되지 않았으면 무시
        if (teleportDestination == null)
        {
            return;
        }

        // 텔레포트 시작
        StartCoroutine(TeleportSequence(other.gameObject));
    }

    /// <summary>
    /// 오브젝트가 플레이어 레이어인지 확인
    /// </summary>
    private bool IsPlayerLayer(GameObject obj)
    {
        return (playerLayer.value & (1 << obj.layer)) != 0;
    }

    /// <summary>
    /// 텔레포트 시퀀스 - 페이드아웃 → 이동 → 페이드인
    /// </summary>
    private IEnumerator TeleportSequence(GameObject player)
    {
        if (sobaekCarSpline != null)
        {
            sobaekCarSpline.Pause();
        }

        // 1. 페이드 아웃 시작
        if (vignetteController != null)
        {
            vignetteController.FadeOut();
        }

        // 2. 페이드 아웃 지속 시간만큼 대기
        yield return new WaitForSeconds(fadeOutDuration);

        // 3. 플레이어의 최상위 부모 찾기
        Transform rootParent = player.transform.root;

        // 4. 최상위 부모를 부모에서 분리 일단 소백카에 플레이어 타있는 채로 이동함
        if (rootParent.parent != null)
        {
            rootParent.SetParent(null, true);
        }

        // 5. 플레이어 위치 이동 (최상위 부모 이동)
        rootParent.position = teleportDestination.position;
        rootParent.rotation = teleportDestination.rotation;

        // 6. 페이드 인 (페이드 아웃 해제)
        if (vignetteController != null)
        {
            vignetteController.FadeIn();
            _playerbleDir.gameObject.SetActive(true);
        }


        yield return new WaitForSeconds(0.5f);
    }
}
