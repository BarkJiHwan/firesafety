#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(TutorialData))]
public class PrefabOffsetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TutorialData script = (TutorialData)target;

        if (GUILayout.Button("MoveZone 위치 복사"))
        {
            var obj = GameObject.Find("MoveZonePoint");
            if (obj != null)
            {
                script.moveZoneOffset = obj.transform.position;
                EditorUtility.SetDirty(script);
            }
            else
            {
                Debug.LogWarning("MoveZonePoint 오브젝트를 찾을 수 없습니다.");
            }
        }

        if (GUILayout.Button("Teawoori 위치 복사"))
        {
            var obj = GameObject.Find("Taewoori (Tutorial)");
            if (obj != null)
            {
                script.teawooriOffset = obj.transform.position;
                script.teawooriRotation = obj.transform.rotation;
                EditorUtility.SetDirty(script);
            }
            else
            {
                Debug.LogWarning("TeawooriPoint 오브젝트를 찾을 수 없습니다.");
            }
        }

        if (GUILayout.Button("Supply 위치 복사"))
        {
            var obj = GameObject.Find("Supply");
            if (obj != null)
            {
                script.supplyOffset = obj.transform.position;
                script.supplyRotation = obj.transform.rotation;
                EditorUtility.SetDirty(script);
            }
            else
            {
                Debug.LogWarning("SupplyPoint 오브젝트를 찾을 수 없습니다.");
            }
        }
    }
}
#endif
