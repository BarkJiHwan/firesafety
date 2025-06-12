using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APTController : MonoBehaviour
{
    [Header("아파트 층별 불 파티클 설정")]
    [SerializeField] private GameObject[] floorFirePrefabs; // 1,2,3,4층 불 파티클 프리팹 배열

    [Header("층별 불 활성화/비활성화")]
    [SerializeField] private bool floor1Fire = false;
    [SerializeField] private bool floor2Fire = false;
    [SerializeField] private bool floor3Fire = false;
    [SerializeField] private bool floor4Fire = false;

    private bool[] previousFireStates; // 이전 상태 저장용

    void Start()
    {
        // 배열 크기 체크
        if (floorFirePrefabs.Length < 4)
        {
            Debug.LogError("불 파티클 프리팹이 4개 미만입니다! 1~4층 프리팹을 모두 할당해주세요.");
            return;
        }

        // 이전 상태 배열 초기화
        previousFireStates = new bool[4];

        // 초기 설정 적용
        UpdateFireParticles();
    }

    void Update()
    {
        // 인스펙터에서 값이 변경되었는지 체크
        CheckForChanges();
    }

    void CheckForChanges()
    {
        bool[] currentFireStates = { floor1Fire, floor2Fire, floor3Fire, floor4Fire };

        // 이전 상태와 비교해서 변경된 것만 업데이트
        for (int i = 0; i < 4; i++)
        {
            if (currentFireStates[i] != previousFireStates[i])
            {
                UpdateFloorFire(i, currentFireStates[i]);
                previousFireStates[i] = currentFireStates[i];
            }
        }
    }

    void UpdateFireParticles()
    {
        bool[] fireStates = { floor1Fire, floor2Fire, floor3Fire, floor4Fire };

        for (int i = 0; i < 4; i++)
        {
            UpdateFloorFire(i, fireStates[i]);
            previousFireStates[i] = fireStates[i];
        }
    }

    void UpdateFloorFire(int floorIndex, bool isActive)
    {
        if (floorFirePrefabs[floorIndex] != null)
        {
            floorFirePrefabs[floorIndex].SetActive(isActive);

            // 디버그 로그
            string floorName = (floorIndex + 1) + "층";
            string status = isActive ? "활성화" : "비활성화";
            Debug.Log($"{floorName} 불 파티클 {status}");
        }
        else
        {
            Debug.LogWarning($"{floorIndex + 1}층 불 파티클 프리팹이 할당되지 않았습니다!");
        }
    }

    // 외부에서 호출할 수 있는 메서드들
    public void SetFloorFire(int floorNumber, bool isActive)
    {
        if (floorNumber < 1 || floorNumber > 4)
        {
            
            return;
        }

        switch (floorNumber)
        {
            case 1:
                floor1Fire = isActive;
                break;
            case 2:
                floor2Fire = isActive;
                break;
            case 3:
                floor3Fire = isActive;
                break;
            case 4:
                floor4Fire = isActive;
                break;
        }

        UpdateFloorFire(floorNumber - 1, isActive);
    }

    public void SetAllFloorsFire(bool isActive)
    {
        floor1Fire = floor2Fire = floor3Fire = floor4Fire = isActive;
        UpdateFireParticles();
    }

    public bool GetFloorFireState(int floorNumber)
    {
        switch (floorNumber)
        {
            case 1:
                return floor1Fire;
            case 2:
                return floor2Fire;
            case 3:
                return floor3Fire;
            case 4:
                return floor4Fire;
            default:
                Debug.LogError("층수는 1~4 사이여야 합니다!");
                return false;
        }
    }
}
