using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// 화재 예방 오브젝트의 정보(이름, 설명 등)를 저장하는 ScriptableObject 데이터 클래스
[CreateAssetMenu(fileName = "PreventableObjData")]
public class PreventableObjData : ScriptableObject
{
    /// <summary>
    /// 예방 아이템 리스트
    /// </summary>
    public List<SafetyItem> items = new List<SafetyItem>();

    /// <summary>
    /// 타입별 아이템 딕셔너리
    /// </summary>
    private Dictionary<PreventType, SafetyItem> _itemDict;

    /// <summary>
    /// ScriptableObject가 활성화될 때 딕셔너리 매핑
    /// </summary>
    private void OnEnable()
    {
        SetDictionary();
    }

    /// <summary>
    /// CSV 파일을 불러와 리스트 초기화
    /// </summary>
    public void LoadCSV()
    {
        items.Clear();
        var csvData = Resources.Load<TextAsset>("safety_items");
        if (csvData == null)
            return;

        string[] lines = csvData.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            string[] values = line.Split('\t');
            if (values.Length < 5)
                continue;

            if (!Enum.TryParse(values[3], out PreventType type))
                continue;

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

    /// <summary>
    /// 리스트를 PreventType 기준 딕셔너리로 매핑
    /// </summary>
    public void SetDictionary() => _itemDict = items.ToDictionary(item => item.Type);

    /// <summary>
    /// 타입에 해당하는 아이템 반환
    /// </summary>
    public SafetyItem GetItem(PreventType type)
    {
        if (_itemDict != null && _itemDict.TryGetValue(type, out SafetyItem item))
        {
            return item;
        }
        return null;
    }
}
