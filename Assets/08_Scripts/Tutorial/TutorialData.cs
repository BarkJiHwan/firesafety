using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Data")]
public class TutorialData : ScriptableObject
{
    // 이동 영역(Zone) 프리팹 관련 변수
    [Header("이동 영역 프리팹")]
    public GameObject moveZonePrefab;      // 이동 영역을 생성할 때 사용할 프리팹 오브젝트
    public Vector3 moveZoneOffset;         // 이동 영역 프리팹의 위치 오프셋(기준 위치에서의 상대적 위치)

    // 전투 관련 프리팹 변수
    [Header("전투 프리팹")]
    public GameObject teawooriPrefab;      // '태우리' 전투 유닛 프리팹 오브젝트
    public Vector3 teawooriOffset;         // '태우리' 프리팹의 위치 오프셋
    public Quaternion teawooriRotation;    // '태우리' 프리팹의 회전값

    public GameObject supplyPrefab;        // 보급품(서플라이) 프리팹 오브젝트
    public Vector3 supplyOffset;           // 보급품 프리팹의 위치 오프셋
    public Quaternion supplyRotation;      // 보급품 프리팹의 회전값

}
