using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ScoreType
{
    Prevention_Count,
    Prevention_Time,
    Fire_Count,
    Fire_Time,
    Elevator,
    Smoke,
    Taewoori_Count,
    DaTaewoori
}

public interface IScorable
{
    void SetScore(ScoreType scoreType, int score);

    float GetScore(ScoreType scoreType);

    bool IsScorePerfect(ScoreType scoreType);
}

