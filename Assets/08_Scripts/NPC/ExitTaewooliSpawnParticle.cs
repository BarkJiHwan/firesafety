using UnityEngine;
using System.Collections;

/// <summary>
/// 불 파티클과 함께 ExitTaewoori를 생성하는 스크립트
/// 부모 오브젝트에 붙이고, 자식으로 파티클들을 둠
/// </summary>
public class ExitTaewooliSpawnParticle : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("태우리 생성 설정")]
    [SerializeField] private GameObject exitTaewooriPrefab;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Header("파티클 페이드아웃 설정")]
    [SerializeField] private float fadeOutDuration = 2f; // 페이드아웃 시간
    #endregion

    #region 변수 선언
    private bool hasSpawned = false;
    private ExitTaewoori spawnedTaewoori;
    private FloorManager floorManager;
    private bool isFadingOut = false;
    #endregion

    #region 프로퍼티
    private Vector3 FinalSpawnPosition => transform.position + spawnOffset;
    private Quaternion FinalSpawnRotation => transform.rotation;
    #endregion

    #region 유니티 라이프사이클

    void Start()
    {
        // FloorManager가 활성화할 때까지 자식 파티클들만 비활성화
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    #endregion

    #region FloorManager 연결
    public void SetFloorManager(FloorManager manager)
    {
        floorManager = manager;
    }

    /// <summary>
    /// FloorManager에서 호출하는 즉시 활성화 메서드
    /// </summary>
    public void ActivateImmediately()
    {
        // 부모와 모든 자식 파티클들 활성화
        gameObject.SetActive(true);

        // 모든 자식 오브젝트 활성화
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
        }

        // 태우리 생성
        if (!hasSpawned)
        {
            SpawnTaewoori();
            hasSpawned = true;
        }
    }
    #endregion

    #region 태우리 생성
    void SpawnTaewoori()
    {
        if (exitTaewooriPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = FinalSpawnPosition;
        Quaternion spawnRot = FinalSpawnRotation;

        GameObject taewooliObj = Instantiate(exitTaewooriPrefab, spawnPos, spawnRot);
        ExitTaewoori taewoori = taewooliObj.GetComponent<ExitTaewoori>();

        if (taewoori != null)
        {
            // 태우리 초기화
            taewoori.Initialize(this);
            spawnedTaewoori = taewoori;

        }
        else
        {
            Destroy(taewooliObj);
        }
    }
    #endregion

    #region 태우리 관리
    /// <summary>
    /// 태우리가 제거될 때 호출 (ExitTaewoori에서 호출)
    /// </summary>
    public void OnTaewooliDestroyed(ExitTaewoori taewoori)
    {
        // FloorManager에 처치 알림
        if (floorManager != null)
        {
            floorManager.OnTaewooliKilled();
        }

        // 참조 정리
        if (spawnedTaewoori == taewoori)
        {
            spawnedTaewoori = null;
        }

        // 파티클 서서히 페이드아웃
        if (!isFadingOut)
        {
            StartCoroutine(FadeOutParticles());
        }
    }

    /// <summary>
    /// 파티클 서서히 페이드아웃
    /// </summary>
    IEnumerator FadeOutParticles()
    {
        isFadingOut = true;

        // 모든 자식 파티클 시스템 찾기
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();

        if (particleSystems.Length > 0)
        {
            // 1단계: 파티클 방출 중단 (새로운 파티클 생성 안함)
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    var emission = ps.emission;
                    emission.enabled = false; // 방출 중단
                }
            }

            // 2단계: 기존 파티클들이 자연스럽게 사라질 때까지 대기
            yield return new WaitForSeconds(fadeOutDuration);
        }

        // 3단계: 완전히 비활성화
        gameObject.SetActive(false);
    }

    #endregion
}
