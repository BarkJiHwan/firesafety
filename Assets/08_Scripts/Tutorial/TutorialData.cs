using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Data")]
public class TutorialData : ScriptableObject
{
    [Header("이동 영역 프리팹")]
    public GameObject moveZonePrefab;
    public Vector3 moveZoneOffset;

    [Header("전투 프리팹")]
    public GameObject teawooriPrefab;
    public Vector3 teawooriOffset;
    public Quaternion teawooriRotation;
    public GameObject supplyPrefab;
    public Vector3 supplyOffset;
    public Quaternion supplyRotation;
}
