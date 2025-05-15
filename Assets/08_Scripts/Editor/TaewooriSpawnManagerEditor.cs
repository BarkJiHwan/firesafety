using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TaewooriSpawnManager))]
public class TaewooriSpawnManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TaewooriSpawnManager manager = (TaewooriSpawnManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("전역 설정", EditorStyles.boldLabel);

        if (GUILayout.Button("모든 오브젝트 콜라이더 중심 사용"))
        {
            manager.SetAllUseColliderCenter(true);
        }

        if (GUILayout.Button("모든 오브젝트 오브젝트 위치 사용"))
        {
            manager.SetAllUseColliderCenter(false);
        }

        EditorGUILayout.Space();
    }
}
