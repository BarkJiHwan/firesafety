using UnityEngine;
using UnityEditor;

/// <summary>
/// ReadOnly 속성을 위한 커스텀 PropertyDrawer
/// 인스펙터에서 읽기 전용으로 표시
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 이전 GUI 상태 저장
        var previousGUIState = GUI.enabled;

        // GUI 비활성화 (읽기 전용)
        GUI.enabled = false;

        // 기본 프로퍼티 그리기
        EditorGUI.PropertyField(position, property, label);

        // 이전 GUI 상태 복원
        GUI.enabled = previousGUIState;
    }
}
