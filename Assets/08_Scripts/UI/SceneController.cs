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
    public SceneType chooseSceneType { get; set; }
    public PlayerEnum charType { get; set; }

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

    public void MoveToCharacterScene()
    {
        LoadScene((int)SceneType.SelectCharacterScene);
    }

    public void MoveToSceneChoose()
    {
        LoadScene((int)SceneType.SceneChooseScene);
    }

    public void MoveToMainScene()
    {
        LoadScene((int)SceneType.MainScene);
    }

    public void MoveToPreventionFireScene()
    {
        LoadScene((int)SceneType.IngameScene_Fire);
    }

    public void MoveToEvacuationScene()
    {
        LoadScene((int)SceneType.IngameScene_Evacuation);
    }

    public void SetChooseCharacterType(PlayerEnum type)
    {
        charType = type;
    }

    public PlayerEnum GetChooseCharacterType()
    {
        return charType;
    }
}
