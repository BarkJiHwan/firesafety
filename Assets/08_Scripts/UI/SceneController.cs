using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneType
{
    MainScene,
    SceneChooseScene,
    SelectCharacterScene,
    IngameScene_Fire,
    IngameScene_Evacuation
}

public class SceneController : MonoBehaviour
{
    static SceneController _instance;
    AsyncOperation oper;

    // 고른 SceneType 필드
    public SceneType chooseSceneType { get; set; }
    public PlayerCharacterSo charType { get; set; }

    public static SceneController Instance
    {
        get
        {
            return _instance;
        }
    }

    void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public AsyncOperation LoadScene(int sceneNum)
    {
        return SceneManager.LoadSceneAsync(sceneNum);
    }

    // 캐릭터 선택창으로 이동
    public void MoveToCharacterScene()
    {
        LoadScene((int)SceneType.SelectCharacterScene);
    }

    // 씬 고르는 곳으로 이동
    public void MoveToSceneChoose()
    {
        LoadScene((int)SceneType.SceneChooseScene);
    }

    // 메인 씬으로 이동
    public void MoveToMainScene()
    {
        LoadScene((int)SceneType.MainScene);
    }

    // 예방/화재 씬으로 이동
    public void MoveToPreventionFireScene()
    {
        LoadScene((int)SceneType.IngameScene_Fire);
    }

    // 탈출 씬으로 이동
    public void MoveToEvacuationScene()
    {
        LoadScene((int)SceneType.IngameScene_Evacuation);
    }

    // 캐릭터 창에서 고른 캐릭터 타입 저장
    public void SetChooseCharacterType(PlayerCharacterSo charInfo)
    {
        charType = charInfo;
    }

    // 캐릭터 창에서 고른 캐릭터 타입 가져오기
    public PlayerCharacterSo GetChooseCharacterType()
    {
        return charType;
    }
}
