using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum DialogueType
{
    Tutorial,
    Sobaek,
}

public class DialogueData
{
    private DialogueType _type;
    private string _id;
    private string _fileName;
    private string _text;

    public DialogueData(DialogueType type, string id, string fileName, string text)
    {
        _type = type;
        _id = id;
        _fileName = fileName;
        _text = text;
    }

    public DialogueType Type => _type;

    public string ID => _id;

    public string FileName => _fileName;

    public string Text => _text;
}

public class DialogueLoader : MonoBehaviour
{
    private readonly string audioFileRoot = "";
    private readonly string audioMetaDataFolder = "AudioFileData/";

    private readonly string metaFileSuffix = ".csv";
    private readonly string audioFileSuffix = ".wav";

    private List<DialogueData> _dialogueList;
    private Dictionary<string, DialogueData> _dialogueDict;

    private void Awake()
    {
        _dialogueList = new List<DialogueData>();
        _dialogueDict = new Dictionary<string, DialogueData>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        ReadTutorialData();
        Debug.Log(_dialogueDict.Count);
    }

    public void ReadTutorialData() => ReadDialogue(DialogueType.Tutorial);

    public void ReadSobaekData() => ReadDialogue(DialogueType.Sobaek);

    public void ReadDialogue(DialogueType type)
    {
        string fileName = "";
        List<DialogueData> dialogueList = new ();

        if (type == DialogueType.Tutorial)
        {
            fileName = DialogueType.Tutorial.ToString();
        }

        if (type == DialogueType.Sobaek)
        {
            fileName = DialogueType.Sobaek.ToString();
        }

        if (fileName == String.Empty)
        {
            Debug.LogWarning("데이터가 없습니다, 확인해주세요");
        }

        // #if UNITY_EDITOR
        // Debug.Log("path : " + "Assets/Resources" + audioMetaDataFolder + fileName);
        // string[] allLines = File.ReadAllLines("Assets/Resources" + audioMetaDataFolder + fileName);
        // #endif

        Debug.Log("path : " + audioMetaDataFolder + fileName);

        TextAsset textAsset = Resources.Load<TextAsset>(audioMetaDataFolder + fileName);
        Debug.Log("TextAsset : " + textAsset.text.Length);

        if (_dialogueList.Count > 0)
        {
            _dialogueList.Clear();
        }

        if (_dialogueDict.Count > 0)
        {
            _dialogueDict.Clear();
        }

        // 첫 번째 라인은 헤더이므로 건너뛰기
        // for (int i = 1; i < allLines.Length; i++)
        // {
        //     string[] row = allLines[i].Split(',');
        //
        //     if (row.Length < 3)
        //     {
        //         Debug.LogWarning(fileName + "파일의 데이터가 없습니다, "  + i + "번쨰 라인이 이상합니다" + " 확인해주세요");
        //     }
        //
        //     DialogueData dialogueData = new DialogueData(type, row[0], row[1], row[2]);
        //
        //     _dialogueList.Add(dialogueData);
        //     _dialogueDict.Add(row[0], dialogueData);
        // }
    }
}
