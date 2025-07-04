using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour, IScorable
{
    [SerializeField] int basicScore = 0;

    Dictionary<ScoreType, int> dicScore = new Dictionary<ScoreType, int>();

    void Start()
    {
        // Dictionary에 모든 ScoreType의 기본 점수와 함께 생성
        for(int i=0; i<(int)ScoreType.End; i++)
        {
            ScoreType type = (ScoreType)i;
            dicScore.Add(type, basicScore);
        }
    }

    // 해당 ScoreType에 따른 스코어 받아오기
    public int GetScore(ScoreType scoreType)
    {
        return dicScore[scoreType];
    }

    // 해당 ScoreType에 따른 스코어 저장
    public void SetScore(ScoreType scoreType, int score)
    {
        dicScore[scoreType] = score;
    }

    // 해당 ScoreType에 따른 스코어가 20점 이상이면 True 반환
    public bool IsScorePerfect(ScoreType scoreType)
    {
        return dicScore[scoreType] >= 20;
    }

    public int SetScoreStep(ScoreType type)
    {
        return dicScore[type];
    }

    // 임시
    public int GetDictionaryCount()
    {
        return dicScore.Count;
    }

    // 해당 인덱스부터 4개의 Score 반환
    public int[] GetScores(int sceneIndex)
    {
        int[] scores = new int[dicScore.Count / 2];

        for(int i=sceneIndex; i<sceneIndex + (dicScore.Count / 2); i++)
        {
            Debug.Log("SceneIdx :" + sceneIndex + " DicScoreCnt : " + dicScore.Count + "index:" + i);
            scores[i-sceneIndex] = dicScore[(ScoreType)i];
        }
        return scores;
    }
}
