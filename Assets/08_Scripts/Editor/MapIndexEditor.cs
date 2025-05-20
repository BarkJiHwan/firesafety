using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
[CustomEditor(typeof(MapIndex))]
public class MapIndexEditor : Editor
{
    private bool showFireObjects = true;
    private bool showPreventables = true;
    private MapIndex mapIndex;

    private void OnEnable()
    {
        mapIndex = (MapIndex)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // MapIndex의 기본 필드 표시
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_mapIndex"));

        // 수동으로 자식 오브젝트 수집 버튼
        if (GUILayout.Button("자식 오브젝트 수집"))
        {
            mapIndex.CollectChildren();
            EditorUtility.SetDirty(mapIndex);
        }

        // FireObjects 폴드아웃 그룹
        EditorGUILayout.Space();
        showFireObjects = EditorGUILayout.Foldout(showFireObjects, $"화재 오브젝트 ({mapIndex.FireObjects.Count}개)", true);
        if (showFireObjects)
        {
            EditorGUI.indentLevel++;
            DisplayObjectList(mapIndex.FireObjects, "화재 오브젝트");
            EditorGUI.indentLevel--;
        }

        // FirePreventables 폴드아웃 그룹
        EditorGUILayout.Space();
        showPreventables = EditorGUILayout.Foldout(showPreventables, $"예방 가능 오브젝트 ({mapIndex.FirePreventables.Count}개)", true);
        if (showPreventables)
        {
            EditorGUI.indentLevel++;
            DisplayObjectList(mapIndex.FirePreventables, "예방 가능 오브젝트");
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DisplayObjectList<T>(List<T> objectList, string emptyMessage) where T : Component
    {
        if (objectList.Count == 0)
        {
            EditorGUILayout.HelpBox(emptyMessage + "가 없습니다.", MessageType.Info);
            return;
        }

        for (int i = 0; i < objectList.Count; i++)
        {
            T obj = objectList[i];
            if (obj == null)
            {
                EditorGUILayout.HelpBox($"항목 {i}: 비어있음", MessageType.Warning);
                continue;
            }

            EditorGUILayout.BeginHorizontal();

            // 오브젝트 필드 (읽기 전용)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField($"항목 {i}", obj.gameObject, typeof(GameObject), true);
            EditorGUI.EndDisabledGroup();

            // 오브젝트로 이동 버튼
            if (GUILayout.Button("선택", GUILayout.Width(60)))
            {
                Selection.activeGameObject = obj.gameObject;
                SceneView.FrameLastActiveSceneView();
            }

            EditorGUILayout.EndHorizontal();

            // FireObjScript인 경우 추가 정보 표시
            if (obj is FireObjScript fireObj)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Toggle("불 상태", fireObj.IsBurning);
                EditorGUI.indentLevel--;
            }
            // FirePreventable인 경우 추가 정보 표시
            else if (obj is FirePreventable preventable)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Toggle("예방 완료", preventable.IsFirePreventable);
                EditorGUI.indentLevel--;
            }

            // 항목 사이 구분선
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
        }
    }
}
#endif
