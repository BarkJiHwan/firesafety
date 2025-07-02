using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    DatabaseReference dbRef;

    void Start()
    {
        InitializeFirebase();
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase 초기화");
            }
            else
            {
                Debug.LogError("Firebase 초기화 실패");
            }
        });
    }

    public void SaveCharacterSelection(string characterName)
    {
        if(dbRef == null)
        {
            Debug.LogWarning("Firebase 아직 초기화 안 됐음");
            return;
        }

        //float timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string timeStamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        string key = dbRef.Child("characterSelections").Push().Key;
        var entryRef = dbRef.Child("characterSelections").Child(key);

        entryRef.Child("character").SetValueAsync(characterName).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"character {characterName} 저장됨");
            }
            else
            {
                Debug.Log($"Error character: {task.Exception}");
            }
        });
        entryRef.Child("timestamp").SetValueAsync(timeStamp).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"timeStamp {timeStamp} 저장됨");
            }
            else
            {
                Debug.Log($"Error timeStamp: {task.Exception}");
            }
        });

        SaveCharacterPreference(characterName);
    }

    void SaveCharacterPreference(string characterName)
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        DatabaseReference dailyCountsRef = dbRef.Child("characterCounts").Child(today).Child(characterName);

        dailyCountsRef.RunTransaction(mutableData =>
        {
            int count = 0;
            if (mutableData.Value != null)
            {
                count = Convert.ToInt32(mutableData.Value);
            }
            mutableData.Value = count + 1;
            return TransactionResult.Success(mutableData);
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"{today} count {characterName} 저장됨");
            }
            else
            {
                Debug.Log($"Error timeStamp: {task.Exception}");
            }
        });
    }

    public void RemoveData()
    {
        dbRef.Child("characterSelections").RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("모든 데이터 삭제 완료");
            }
            else
            {
                Debug.LogError($"Error 못 지웠음 : {task.Exception}");
            }
        });

        dbRef.Child("dailyCharacterCounts").RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("모든 데이터 삭제 완료");
            }
            else
            {
                Debug.LogError($"Error 못 지웠음 : {task.Exception}");
            }
        });
    }
}
