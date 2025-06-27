using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour, IScorable
{
    [SerializeField] int basicScore = 0;

    Dictionary<ScoreType, int> dicScore = new Dictionary<ScoreType, int>();

    void Start()
    {
        for(int i=0; i<(int)ScoreType.End; i++)
        {
            ScoreType type = (ScoreType)i;
            dicScore.Add(type, basicScore);
        }
    }

    public int GetScore(ScoreType scoreType)
    {
        return dicScore[scoreType];
    }

    public void SetScore(ScoreType scoreType, int score)
    {
        dicScore[scoreType] = score;
    }

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

    public int[] GetScores(int sceneIndex)
    {
        int[] scores = new int[dicScore.Count / 2];
        int index = 0;
        //foreach(int score in dicScore.Values)
        //{
        //    scores[index] = score;
        //    index++;
        //}
        for(int i=sceneIndex; i<sceneIndex + (dicScore.Count / 2); i++)
        {
            scores[i] = dicScore[(ScoreType)i];
        }
        return scores;
    }
}
