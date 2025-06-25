using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitSupplyManager : MonoBehaviour
{
    GameObject[] exitNecessity;

    GameObject fireAlarm;

    int[] matsIndex;
    int newMatsCount;

    void Start()
    {
        exitNecessity = GameObject.FindGameObjectsWithTag("ExitNecessity");
        ChangeMaterial();
        MakeFireAlarm();
    }

    void Update()
    {
        
    }

    void ChangeMaterial()
    {
        matsIndex = new int[2];
        int index = 0;
        for (int i=0; i<exitNecessity.Length; i++)
        {
            GameObject exit = exitNecessity[i];
            if (exit.GetComponent<Phase2_FireAlram>() != null)
            {
                fireAlarm = exit;
            }
            else
            {
                Renderer rend = exit.GetComponent<Renderer>();
                Material mat = rend.material;
                Texture baseTexture = mat.GetTexture("_BaseMap");
                rend.materials = MakeNewMaterial(baseTexture);
                matsIndex[index] = i;
                index++;
                SetNearPlayerActive(exit, true);
            }
        }
    }

    Material[] MakeNewMaterial(Texture texture)
    {
        Material[] mats = new Material[2];
        mats[0] = Resources.Load<Material>("Materials/OutlineMat");
        mats[1] = Resources.Load<Material>("Materials/OriginMat");
        mats[1].SetTexture("_PreventTexture", texture);
        mats[1].SetFloat("_RimPower", 2f);
        newMatsCount = mats.Length;
        return mats;
    }

    void SetNearPlayerActive(GameObject obj, bool isActive)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        Material[] mats = rend.materials;
        foreach(Material mat in mats)
        {
            if(mat.HasProperty("_isNearPlayer"))
            {
                mat.SetFloat("_isNearPlayer", isActive ? 1f : 0f);
            }
        }
    }

    public void SetFireAlarmMat(bool isActive)
    {
        SetNearPlayerActive(fireAlarm.transform.GetChild(0).gameObject, isActive);
    }

    public void SetTowelAndWater(bool isActive)
    {
        for(int i=0; i< matsIndex.Length; i++)
        {
            SetNearPlayerActive(exitNecessity[matsIndex[i]], isActive);
        }
    }

    void MakeFireAlarm()
    {
        GameObject child = fireAlarm.transform.GetChild(0).gameObject;
        Renderer rend = child.GetComponent<Renderer>();
        Material[] originMats = new Material[rend.materials.Length];
        originMats = rend.materials;

        Texture baseTexture = originMats[3].GetTexture("_BaseMap");
        Material newMat = new Material(Resources.Load<Material>("Materials/OriginMat"));
        newMat.SetTexture("_PreventTexture", baseTexture);
        newMat.SetFloat("_RimPower", 2f);
        Material[] highlightMats = new Material[newMatsCount];
        highlightMats = MakeNewMaterial(Resources.Load<Texture2D>("Materials/FireAlarmColor"));
        Material[] newMats = new Material[]
        {
            originMats[0],
            originMats[2],
            highlightMats[1],
            originMats[1],
            newMat,
            highlightMats[0]
        };
        rend.materials = newMats;

        SetNearPlayerActive(child, false);
    }
}
