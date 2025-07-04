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
    // Firebase 실시간 데이터베이스의 루트 참조
    DatabaseReference dbRef;

    void Start()
    {
        // Firebase 초기화
        InitializeFirebase();
    }

    void InitializeFirebase()
    {
        // Firebase 의존성 확인 및 초기화
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase 연결 성공 시 루트 참조 저장
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase 초기화");
            }
            else
            {
                Debug.LogError("Firebase 초기화 실패");
            }
        });
    }

    // 캐릭터 선택 정보를 저장하는 함수
    public void SaveCharacterSelection(string characterName)
    {
        if(dbRef == null)
        {
            Debug.LogWarning("Firebase 아직 초기화 안 됐음");
            return;
        }

        // 현재 시간 (UTC 기준) 타임스탬프 문자열 생성
        //float timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string timeStamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        // 고유 키 생성
        string key = dbRef.Child("characterSelections").Push().Key;
        var entryRef = dbRef.Child("characterSelections").Child(key);

        // 캐릭터 이름 저장
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
        // 타임스탬프 저장
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

        // 선택된 캐릭터의 통계 저장
        SaveCharacterPreference(characterName);
    }

    // 캐릭터 선택 수를 날짜별로 누적 저장하는 함수
    void SaveCharacterPreference(string characterName)
    {
        // 오늘 날짜
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        // 해당 날짜와 캐릭터 이름 경로로 참조 생성
        DatabaseReference dailyCountsRef = dbRef.Child("characterCounts").Child(today).Child(characterName);

        // 현재 카운트를 읽고 +1 후 저장 (트랜잭션 사용)
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

    // 모든 저장된 캐릭터 데이터 및 통계를 삭제하는 함수
    public void RemoveData()
    {
        // 선택된 캐릭터 로그 전체 삭제
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

        // 통계 데이터 전체 삭제
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
