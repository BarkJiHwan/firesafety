using System.Collections;
using UnityEngine;

/// <summary>
/// 층별 타입 정의
/// </summary>
public enum FloorEventType
{
    Nothing,            // 아무것도 안함
    TaewooliWithFire,  // 태우리 + 불 (4,2층)
    SmokeOnly,         // 연기만 (3층)
    FireOnly,          // 불만 (1층)
}

/// <summary>
/// 층 전체를 관리하는 매니저
/// </summary>
public class FloorManager : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("층 기본 설정")]
    [SerializeField] private int floorNumber = 4;
    [SerializeField] private FloorEventType floorEventType = FloorEventType.Nothing;

    [Header("웨이포인트 설정")]
    [SerializeField] private GameObject startWaypoint;
    [SerializeField] private GameObject endWaypoint;

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
    private static bool isInitialized = false;

    // 점수 관리
    private int taewooliKillCount = 0;
    private static int totalTaewooliKills = 0;
    private ScoreManager scoreManager;
    #endregion

    #region 유니티 라이프사이클
    void Start()
    {
        // 최초 한 번만 초기화
        if (!isInitialized)
        {
            InitializeAllFloors();
            isInitialized = true;
        }

        // ScoreManager 자동 찾기
        scoreManager = FindObjectOfType<ScoreManager>();

        // 웨이포인트 설정
        SetupWaypoints();
    }

    /// <summary>
    /// 모든 층 초기화 - 4층만 활성화
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
    /// 웨이포인트 트리거 설정
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
        }

        if (endWaypoint != null)
        {
            WaypointTrigger endTrigger = endWaypoint.GetComponent<WaypointTrigger>();
            if (endTrigger == null)
            {
                endTrigger = endWaypoint.AddComponent<WaypointTrigger>();
            }
            endTrigger.Initialize(this, WaypointType.End);
        }
    }
    #endregion

    #region 층 활성화/비활성화
    public void ActivateFloor()
    {
        isActive = true;
        floorCompleted = false;
        taewooliKillCount = 0;

        // 시작점만 활성화
        if (startWaypoint != null)
            startWaypoint.SetActive(true);

        if (endWaypoint != null)
            endWaypoint.SetActive(false);

        // 파티클 그룹 비활성화
        if (allParticleGroup != null)
            allParticleGroup.SetActive(false);

        startTriggered = false;
        endTriggered = false;
    }

    public void DeactivateFloor()
    {
        isActive = false;
        floorCompleted = true;

        // 스폰 중단
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        // 웨이포인트 비활성화
        if (startWaypoint != null)
            startWaypoint.SetActive(false);
        if (endWaypoint != null)
            endWaypoint.SetActive(false);

        // 파티클 그룹 비활성화
        if (allParticleGroup != null)
            allParticleGroup.SetActive(false);
    }
    #endregion

    #region 웨이포인트 이벤트
    public void OnStartWaypointTriggered()
    {
        if (!isActive || startTriggered || floorCompleted)
            return;

        startTriggered = true;

        // 스폰 시퀀스 시작
        spawnCoroutine = StartCoroutine(SpawnSequence());

        // 끝점 활성화
        if (endWaypoint != null)
        {
            endWaypoint.SetActive(true);
        }
    }

    public void OnEndWaypointTriggered()
    {
        if (!isActive || endTriggered || floorCompleted)
            return;

        endTriggered = true;
        floorCompleted = true;

        // 스폰 중단
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        // 1층에서만 최종 점수 계산
        if (floorNumber == 1)
        {
            SendTotalScore();
        }

        // 현재 층 정리
        CleanupFloor();

        // 다음 층 활성화
        if (nextFloorManager != null)
        {
            nextFloorManager.ActivateFloor();
        }

        // 현재 층 비활성화
        DeactivateFloor();
    }
    #endregion

    #region 점수 관리
    public void OnTaewooliKilled()
    {
        taewooliKillCount++;
        totalTaewooliKills++;
    }

    void SendTotalScore()
    {
        if (scoreManager == null)
            return;

        int killScore = CalculateKillScore(totalTaewooliKills);
        scoreManager.SetScore(ScoreType.Taewoori_Count, killScore);
    }

    int CalculateKillScore(int totalKills)
    {
        if (totalKills >= 8)
            return 25;      // 8마리 (전부)
        else if (totalKills >= 4)
            return 20;      // 4마리 이상
        else
            return 15;      // 4마리 미만
    }
    #endregion

    #region 스폰 시퀀스
    IEnumerator SpawnSequence()
    {
        // 1단계: 파티클 딜레이
        if (particleStartDelay > 0)
        {
            yield return new WaitForSeconds(particleStartDelay);
        }

        if (floorCompleted)
            yield break;

        // 2단계: 파티클 그룹 활성화
        if (allParticleGroup != null)
        {
            allParticleGroup.SetActive(true);

            // 태우리 스포너들 찾기 및 FloorManager 연결
            taewooliSpawners = allParticleGroup.GetComponentsInChildren<ExitTaewooliSpawnParticle>();
            foreach (var spawner in taewooliSpawners)
            {
                if (spawner != null)
                {
                    spawner.SetFloorManager(this);
                }
            }
        }

        // 3단계: 태우리 시작 딜레이
        if (taewooliStartDelay > 0)
        {
            yield return new WaitForSeconds(taewooliStartDelay);
        }

        if (floorCompleted)
            yield break;

        // 4단계: 태우리 순차 생성
        if (taewooliSpawners != null && taewooliSpawners.Length > 0)
        {
            for (int i = 0; i < taewooliSpawners.Length; i++)
            {
                if (floorCompleted)
                    yield break;

                if (taewooliSpawners[i] != null)
                {
                    taewooliSpawners[i].ActivateImmediately();

                    // 마지막이 아니면 간격 대기
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

    void CleanupFloor()
    {
        // 생성된 태우리들 삭제
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
