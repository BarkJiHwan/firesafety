using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour, IScorable
{
    Dictionary<ScoreType, float> dicScore = new Dictionary<ScoreType, float>();

    public float GetScore(ScoreType scoreType)
    {
        return dicScore[scoreType];
    }

    public void SetScore(ScoreType scoreType, float score)
    {
        dicScore.Add(scoreType, score);
    }

    public bool IsScorePerfect(ScoreType scoreType)
    {
        return dicScore[scoreType] >= 15;
    }

    public float SetScoreStep(ScoreType type)
    {
        return dicScore[type];
    }
}
