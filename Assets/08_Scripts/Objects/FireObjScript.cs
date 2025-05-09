using System.Collections;
using System.Collections.Generic;
using Photon.Pun.Demo.PunBasics;
using UnityEngine;

public class FireObjScript : MonoBehaviour
{
    [Header("true일 때 태우리 생성 가능 상태"),Tooltip("체크가 되어 있으면 트루입니다.")]
    [SerializeField] private bool _isBurning;
    [SerializeField] private float _fireElementalSummonTime; //태우리 생성 타이머
    [Header("최소 거리"), Tooltip("태우리 생성 오브젝트 기준 최소거리를 설정할 수 있다.\n 5 ~ 15")
        , Range(0.1f, 0.15f)]
    [SerializeField] private float minDis = 0.1f;
    [Header("최대 거리"), Tooltip("태우리 생성 오브젝트 기준 최대거리를 설정할 수 있다.\n 6 ~ 25")
        , Range(0.1f, 0.25f)]
    [SerializeField] private float maxDis = 0.1f;

    public GameObject testObj;
    public bool IsBurning
    {
        get => _isBurning;
        set => _isBurning = value;
    }
    public float FireElementalSummonTime
    {
        get => _fireElementalSummonTime;
        set => _fireElementalSummonTime = value;
    }

    private void Start()
    {
        //IsBurning = false;
        StartCoroutine(CanBurn());
    }

    private void Update()
    {
        //if (isBurning)
        //{
        //    // 스포너 위치에 태우리 소환
        //}
    }

    private IEnumerator CanBurn()
    {
        while (IsBurning)
        {

            yield return new WaitForSeconds(FireElementalSummonTime);
            //태우리 생성
            SpawnFire();
            // 가져오긴 가져올 건데... 태우리를 해당 오브젝트의 자식으로 생성?
            // 또는 그냥 생성
        }
        yield return null;
    }
    void SpawnFire()
    {
        Vector3 spawnPos = CreateAroundPlayer();//지정 위치
        Instantiate(testObj, spawnPos, Quaternion.identity);
        Debug.Log(spawnPos + "위치에 태우리 생성!");
        //태우리 소환 메서드
    }
    
    //해당 오브젝트 주변 반경을 기준으로 랜덤한 위치에 태우리 리젠
    private Vector3 CreateAroundPlayer()
    {
        float angle = Random.Range(0, Mathf.PI * 2);
        float distance = Random.Range(minDis, maxDis);
        return transform.position
            + (new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance);
    }
}
