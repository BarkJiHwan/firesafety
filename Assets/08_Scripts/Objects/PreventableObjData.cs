using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PreventableObjData")]
public class PreventableObjData : ScriptableObject
{
    public List<SafetyItem> items = new List<SafetyItem>();
    private Dictionary<PreventType, SafetyItem> _itemDict;

    private void OnEnable()
    {
        Debug.Log("스크립터블 오브젝트 리스트 딕셔너리 매핑완료");
        SetDictionary();
    }
    // CSV 파일 로드 및 딕셔너리 초기화
    public void LoadCSV()
    {
        items.Clear();
        var csvData = Resources.Load<TextAsset>("safety_items");
        if (csvData == null)
        {
            Debug.LogError("CSV 파일 없음: Resources/safety_items");
            return;
        }

        string[] lines = csvData.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            string[] values = line.Split('\t');
            if (values.Length < 5)
            {
                Debug.LogError($"{i}번째 줄 데이터 부족: {values.Length}/5");
                continue;
            }

            if (!Enum.TryParse(values[3], out PreventType type))
            {
                Debug.LogError($"{i}번째 줄 타입 오류: {values[3]}");
                continue;
            }

            SafetyItem item = new SafetyItem
            {
                ID = values[0],
                Type = type,
                Name = values[1],
                Location = values[2],
                EnglishName = values[3],
                Description = values[4],
            };
            items.Add(item);
        }
    }

    public void SetDictionary() => _itemDict = items.ToDictionary(item => item.Type);

    public SafetyItem GetItem(PreventType type)
    {
        if (_itemDict != null && _itemDict.TryGetValue(type, out SafetyItem item))
        {
            return item;
        }
        return null;
    }
}
