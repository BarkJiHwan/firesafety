#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PreventableObjData))]
public class PreventableObjDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        PreventableObjData data = (PreventableObjData)target;

        if(GUILayout.Button("CSV 로드"))
        {
            data.LoadCSV();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
