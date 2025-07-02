using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phase2_BombPoolManager : MonoBehaviour
{
    public static Phase2_BombPoolManager Instance { get; private set; }

    public Phase2_GenericPooler bigBombPool;
    public Phase2_GenericPooler smallBombPool;

    private void Awake() => Instance = this;

    public GameObject GetBigBomb() => bigBombPool.GetObject();

    public GameObject GetSmallBomb() => smallBombPool.GetObject();

    public void ReturnBomb(GameObject bomb, bool isBig)
    {
        if (isBig)
        {
            bigBombPool.ReturnObject(bomb);
        }
        else
        {
            smallBombPool.ReturnObject(bomb);
        }
    }
}
