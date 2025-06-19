using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueDataType
{
    Tutorial,
    Sobaek,
}

public class DialogueData
{
    private DialogueDataType _type;
    private string _id;
    private string _fileName;
    private string _text;

    public DialogueData(DialogueDataType type, string id, string fileName, string text)
    {
        _type = type;
        _id = id;
        _fileName = fileName;
        _text = text;
    }

    public DialogueDataType Type => _type;

    public string ID => _id;

    public string FileName => _fileName;

    public string Text => _text;
    public override string ToString() => "type : " + _type + " id : " + _id + " fileName : " + _fileName + " text : " + _text;
}

public class DialogueLoader : MonoBehaviour
{
    private readonly string _audioMetaDataFolder = "AudioFileData/";

    private DialogueDataType _currentDialogueType;
    private List<DialogueData> _dialogueList;
    private Dictionary<string, DialogueData> _dialogueDict;
    private Dictionary<string, AudioClip> _audioDict;

    public DialogueDataType CurrentDialogueType => _currentDialogueType;
    public List<DialogueData> DialogueList => _dialogueList;
    public Dictionary<string, DialogueData> DialogueDict => _dialogueDict;
    public Dictionary<string, AudioClip> AudioDict
    {
        get => _audioDict;
    }

    private void Awake()
    {
        _dialogueList = new List<DialogueData>();
        _dialogueDict = new Dictionary<string, DialogueData>();
        _audioDict = new Dictionary<string, AudioClip>();
    }

    // 처음은 튜토리얼 대화 흐름 읽어오기
    private void Start()
    {

        LoadTutorialData();
    }

    private IEnumerator WaitForTest(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DialoguePlayer dialoguePlayer = FindObjectOfType<DialoguePlayer>();
        dialoguePlayer.Stop();
    }

    public void LoadTutorialData() => LoadDialogue(DialogueDataType.Tutorial);
    public void LoadSobaekData() => LoadDialogue(DialogueDataType.Sobaek);

    public void LoadDialogue(DialogueDataType type)
    {
        string fileName = "";

        if (type == DialogueDataType.Tutorial)
        {
            fileName = DialogueDataType.Tutorial.ToString();
        }

        if (type == DialogueDataType.Sobaek)
        {
            fileName = DialogueDataType.Sobaek.ToString();
        }

        if (fileName == string.Empty)
        {
            Debug.LogWarning("데이터가 없습니다, 확인해주세요");
            return;
        }

        _currentDialogueType = type;

        // 로드할떄 기존 데이터가 있었다면 삭제
        ClearDialogueData();
        ParseCSV(type, fileName);
    }

    public void ClearDialogueData()
    {
        if (DialogueList.Count > 0)
        {
            DialogueList.Clear();
        }

        if (DialogueDict.Count > 0)
        {
            DialogueDict.Clear();
        }

        if (AudioDict.Count > 0)
        {
            AudioDict.Clear();
        }
    }

    public void ParseCSV(DialogueDataType type, string fileName)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(_audioMetaDataFolder + fileName);
        string[] allLines = textAsset.text.Split('\n');

        // 첫 번째 라인은 헤더이므로 건너뛰기
        for (int i = 1; i < allLines.Length; i++)
        {
            string[] row = allLines[i].Split(',');

            if (row.Length != 3)
            {
                Debug.LogWarning(fileName + "파일의 데이터가 없습니다, "  + i + "번쨰 라인이 이상합니다" + " 확인해주세요");
            }

            DialogueData dialogueData = new DialogueData(type, row[0], row[1], row[2]);
            DialogueList.Add(dialogueData);
            DialogueDict.Add(row[0], dialogueData);

            AudioClip audioSource = Resources.Load<AudioClip>(_audioMetaDataFolder + type + "/" + row[1]);
            AudioDict.Add(row[0], audioSource);
        }
    }

    public DialogueData GetDialogue(string dialogueId)
    {
        if (DialogueDict == null || !DialogueDict.ContainsKey(dialogueId))
        {
            Debug.LogWarning("대화 데이터의 아이디가 없습니다");
            return null;
        }

        return DialogueDict[dialogueId];
    }

    public AudioClip GetAudioClip(string dialogueId)
    {
        if (AudioDict == null || !AudioDict.ContainsKey(dialogueId))
        {
            Debug.LogWarning("오디오 소스의 아이디가 없습니다");
            return null;
        }

        if (AudioDict[dialogueId] == null)
        {
            Debug.LogWarning("오디오 소스가 널입니다");
            return null;
        }

        return AudioDict[dialogueId];
    }

    public string GetDialogueText(string dialogueId)
    {
        DialogueData dialogue = GetDialogue(dialogueId);

        if (dialogue == null || string.Empty == dialogue.Text)
        {
            Debug.LogWarning("대화 텍스트가 없습니다");
            return "";
        }

        string text = dialogue.Text.Replace("\\n", "\n");

        return text;
    }
}
