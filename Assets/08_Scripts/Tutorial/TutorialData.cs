using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[CreateAssetMenu(menuName = "Tutorial/Data")]
public class TutorialData : ScriptableObject
{
    [Header("이동 영역 프리팹")]
    public GameObject moveZonePrefab;

    [Header("상호작용 오브젝트")]
    public XRSimpleInteractable preventableObj;

    [Header("전투 프리팹")]
    public GameObject teawooriPrefab;
    public GameObject supplyPrefab;

}
