using System.Collections;
using UnityEngine;

public enum FloorEventType
{
    Nothing,
    TaewooliWithFire,
    SmokeOnly,
    FireOnly,
}

/// <summary>
/// 층별 관리 시스템 - 웨이포인트 기반 태우리 스폰 및 진행 관리
/// </summary>
public class FloorManager : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("층 기본 설정")]
    [SerializeField] private int floorNumber = 4;

    [Header("웨이포인트 설정")]
    [SerializeField] private GameObject startWaypoint;
    [SerializeField] private GameObject endWaypoint;
    [SerializeField] private LayerMask playerLayerMask = 1 << 0; // 플레이어 레이어 설정

    [Header("파티클 그룹")]
    [SerializeField] private GameObject allParticleGroup;

    [Header("스폰 타이밍")]
    [SerializeField] private float particleStartDelay = 0f;
    [SerializeField] private float taewooliStartDelay = 10f;
    [SerializeField] private float taewooliSpawnInterval = 3f;

    [Header("다음 층 연결")]
    [SerializeField] private FloorManager nextFloorManager;
    #endregion

    #region 변수 선언
    private bool startTriggered = false;
    private bool endTriggered = false;
    private bool isActive = false;
    private bool floorCompleted = false;
    private ExitTaewooliSpawnParticle[] taewooliSpawners;
    private Coroutine spawnCoroutine;
    private bool isInitialized = false;

    private int taewooliKillCount = 0;
    private int totalTaewooliKills = 0;
    private ScoreManager scoreManager;
    #endregion

    #region 유니티 라이프사이클
    /// <summary>
    /// 초기화 및 웨이포인트 설정
    /// </summary>
    void Start()
    {
        if (!isInitialized)
        {
            InitializeAllFloors();
            isInitialized = true;
        }

        scoreManager = FindObjectOfType<ScoreManager>();
        SetupWaypoints();
    }

    /// <summary>
    /// 모든 층 초기 상태 설정 (4층만 활성화)
    /// </summary>
    void InitializeAllFloors()
    {
        FloorManager[] allFloors = FindObjectsOfType<FloorManager>(true);

        foreach (var floor in allFloors)
        {
            if (floor.floorNumber == 4)
            {
                floor.ActivateFloor();
            }
            else
            {
                floor.DeactivateFloor();
            }
        }
    }

    /// <summary>
    /// 웨이포인트 트리거 컴포넌트 설정
    /// </summary>
    void SetupWaypoints()
    {
        if (startWaypoint != null)
        {
            WaypointTrigger startTrigger = startWaypoint.GetComponent<WaypointTrigger>();
            if (startTrigger == null)
            {
                startTrigger = startWaypoint.AddComponent<WaypointTrigger>();
            }
            startTrigger.Initialize(this, WaypointType.Start);
            startTrigger.SetPlayerLayerMask(playerLayerMask); // 레이어마스크 설정
        }

        if (endWaypoint != null)
        {
            WaypointTrigger endTrigger = endWaypoint.GetComponent<WaypointTrigger>();
            if (endTrigger == null)
            {
                endTrigger = endWaypoint.AddComponent<WaypointTrigger>();
            }
            endTrigger.Initialize(this, WaypointType.End);
            endTrigger.SetPlayerLayerMask(playerLayerMask); // 레이어마스크 설정
        }
    }
    #endregion

    #region 층 활성화/비활성화
    /// <summary>
    /// 층 활성화 - 시작 웨이포인트만 활성화
    /// </summary>
    public void ActivateFloor()
    {
        isActive = true;
        floorCompleted = false;
        taewooliKillCount = 0;

        if (startWaypoint != null)
            startWaypoint.SetActive(true);

        if (endWaypoint != null)
            endWaypoint.SetActive(false);

        if (allParticleGroup != null)
            allParticleGroup.SetActive(false);

        startTriggered = false;
        endTriggered = false;
    }

    /// <summary>
    /// 층 비활성화 - 모든 요소 비활성화
    /// </summary>
    public void DeactivateFloor()
    {
        isActive = false;
        floorCompleted = true;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (startWaypoint != null)
            startWaypoint.SetActive(false);
        if (endWaypoint != null)
            endWaypoint.SetActive(false);

        if (allParticleGroup != null)
            allParticleGroup.SetActive(false);
    }
    #endregion

    #region 웨이포인트 이벤트
    /// <summary>
    /// 시작 웨이포인트 트리거 시 스폰 시퀀스 시작
    /// </summary>
    public void OnStartWaypointTriggered()
    {
        if (!isActive || startTriggered || floorCompleted)
            return;

        startTriggered = true;
        spawnCoroutine = StartCoroutine(SpawnSequence());

        if (endWaypoint != null)
        {
            endWaypoint.SetActive(true);
        }
    }

    /// <summary>
    /// 종료 웨이포인트 트리거 시 다음 층으로 진행
    /// </summary>
    public void OnEndWaypointTriggered()
    {
        if (!isActive || endTriggered || floorCompleted)
            return;

        endTriggered = true;
        floorCompleted = true;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (floorNumber == 1)
        {
            SendTotalScore();
        }

        CleanupFloor();

        if (nextFloorManager != null)
        {
            nextFloorManager.ActivateFloor();
        }

        DeactivateFloor();
    }
    #endregion

    #region 점수 관리
    /// <summary>
    /// 태우리 처치 시 카운트 증가
    /// </summary>
    public void OnTaewooliKilled()
    {
        taewooliKillCount++;
        totalTaewooliKills++;
    }

    /// <summary>
    /// 총 점수 계산 및 전송 (1층 도달 시)
    /// </summary>
    void SendTotalScore()
    {
        if (scoreManager == null)
            return;

        int killScore = CalculateKillScore(totalTaewooliKills);
        scoreManager.SetScore(ScoreType.Taewoori_Count, killScore);
    }

    /// <summary>
    /// 처치 수에 따른 점수 계산
    /// </summary>
    int CalculateKillScore(int totalKills)
    {
        if (totalKills >= 8)
            return 25;
        else if (totalKills >= 4)
            return 20;
        else
            return 15;
    }
    #endregion

    #region 스폰 시퀀스
    /// <summary>
    /// 파티클 및 태우리 스폰 시퀀스 실행
    /// </summary>
    IEnumerator SpawnSequence()
    {
        if (particleStartDelay > 0)
        {
            yield return new WaitForSeconds(particleStartDelay);
        }

        if (floorCompleted)
            yield break;

        if (allParticleGroup != null)
        {
            allParticleGroup.SetActive(true);

            taewooliSpawners = allParticleGroup.GetComponentsInChildren<ExitTaewooliSpawnParticle>();
            foreach (var spawner in taewooliSpawners)
            {
                if (spawner != null)
                {
                    spawner.SetFloorManager(this);
                }
            }
        }

        if (taewooliStartDelay > 0)
        {
            yield return new WaitForSeconds(taewooliStartDelay);
        }

        if (floorCompleted)
            yield break;

        if (taewooliSpawners != null && taewooliSpawners.Length > 0)
        {
            for (int i = 0; i < taewooliSpawners.Length; i++)
            {
                if (floorCompleted)
                    yield break;

                if (taewooliSpawners[i] != null)
                {
                    taewooliSpawners[i].ActivateImmediately();

                    if (i < taewooliSpawners.Length - 1)
                    {
                        float waitTime = 0f;
                        while (waitTime < taewooliSpawnInterval && !floorCompleted)
                        {
                            waitTime += Time.deltaTime;
                            yield return null;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 층 완료 시 모든 태우리 정리
    /// </summary>
    void CleanupFloor()
    {
        ExitTaewoori[] allTaewoori = FindObjectsOfType<ExitTaewoori>();
        foreach (var taewoori in allTaewoori)
        {
            if (taewoori != null)
            {
                Destroy(taewoori.gameObject);
            }
        }
    }
    #endregion
}
