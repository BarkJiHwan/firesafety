using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneType
{
    MainScene,
    SelectCharacterScene,
    Tutorial,
    IngameScene_Fire,
    IngameScene_Evaciaton
}

public class SceneController : MonoBehaviour
{
    static SceneController _instance;
    AsyncOperation oper;

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

}
