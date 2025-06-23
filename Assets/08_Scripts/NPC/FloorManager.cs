using System.Collections;
using UnityEngine;

public enum FloorEventType
{
    Nothing,
    TaewooliWithFire,
    SmokeOnly,
    FireOnly,
}

public class FloorManager : MonoBehaviour
{
    #region 인스펙터 설정
    [Header("층 기본 설정")]
    [SerializeField] private int floorNumber = 4;
    [SerializeField] private FloorEventType floorEventType = FloorEventType.Nothing;

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
    private static bool isInitialized = false;

    private int taewooliKillCount = 0;
    private static int totalTaewooliKills = 0;
    private ScoreManager scoreManager;
    #endregion

    #region 유니티 라이프사이클
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
            return 25;
        else if (totalKills >= 4)
            return 20;
        else
            return 15;
    }
    #endregion

    #region 스폰 시퀀스
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
