using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitSupplyManager : MonoBehaviour
{
    // 탈출 관련 필수 오브젝트
    GameObject[] exitNecessity;
    // 소화전 오브젝트 참조
    GameObject fireAlarm;

    int[] matsIndex;
    int newMatsCount;

    void Start()
    {
        exitNecessity = GameObject.FindGameObjectsWithTag("ExitNecessity");
        // 소화전 외 탈출 관련 필수 오브젝트들의 Material 변경
        ChangeMaterial();
        // 소화전의 Material 구성 변경
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
            // 해당 오브젝트가 소화전일 경우 
            if (exit.GetComponent<Phase2_FireAlram>() != null)
            {
                // fireAlarm에 해당 오브젝트 저장
                fireAlarm = exit;
            }
            else
            {
                // 소화전이 아닐 경우 빛나는 것과 아웃라인 Material로 교체
                Renderer rend = exit.GetComponent<Renderer>();
                Material mat = rend.material;
                // 기존에 Material에 적용된 Texture 저장
                Texture baseTexture = mat.GetTexture("_BaseMap");
                // 저장한 기본 Texture 빛나는 Material의 Texture 저장해서 Material 교체
                rend.materials = MakeNewMaterial(baseTexture);
                matsIndex[index] = i;
                index++;
                // 빛나는 것과 Material 활성화 (일단 플레이어가 가까이 있냐 없냐와 상관없이 활성화)
                SetNearPlayerActive(exit, true);
            }
        }
    }

    Material[] MakeNewMaterial(Texture texture)
    {
        // Outline과 빛나는 거 Material 구성
        Material[] mats = new Material[2];
        mats[0] = Resources.Load<Material>("Materials/OutlineMat");
        mats[1] = Resources.Load<Material>("Materials/OriginMat");
        // 원본 텍스처 복사
        mats[1].SetTexture("_PreventTexture", texture);
        // 하이라이트 강도 설정
        mats[1].SetFloat("_RimPower", 2f);
        newMatsCount = mats.Length;
        return mats;
    }

    void SetNearPlayerActive(GameObject obj, bool isActive)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        Material[] mats = rend.materials;
        // _isNearPlayer 속성으로 빛나는 것, Outline 키고 / 끄기
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
        // 소화전 자식 오브젝트 빛나는 것과 Outline 활성화/비활성화
        SetNearPlayerActive(fireAlarm.transform.GetChild(0).gameObject, isActive);
    }

    public void SetTowelAndWater(bool isActive)
    {
        // 수건과 수전에 빛나는 것과 Outline 활성화/비활성화
        for(int i=0; i< matsIndex.Length; i++)
        {
            SetNearPlayerActive(exitNecessity[matsIndex[i]], isActive);
        }
    }

    void MakeFireAlarm()
    {
        // 소화전 자식 오브젝트 참조
        GameObject child = fireAlarm.transform.GetChild(0).gameObject;
        Renderer rend = child.GetComponent<Renderer>();
        Material[] originMats = new Material[rend.materials.Length];
        originMats = rend.materials;
        // 원래 텍스처를 기반으로 새 메테리얼 생성
        Texture baseTexture = originMats[3].GetTexture("_BaseMap");
        Material newMat = new Material(Resources.Load<Material>("Materials/OriginMat"));
        newMat.SetTexture("_PreventTexture", baseTexture);
        newMat.SetFloat("_RimPower", 2f);
        // 빛나는 Material의 Texture에 해당 색상과 맞는 Texture를 넣기
        Material[] highlightMats = new Material[newMatsCount];
        highlightMats = MakeNewMaterial(Resources.Load<Texture2D>("Materials/FireAlarmColor"));
        // 소화전 오브젝트 새 메테리얼 조합 적용
        Material[] newMats = new Material[]
        {
            originMats[0],      // 기존 Material 중 하나
            originMats[2],      // 기존 Material 중 하나
            highlightMats[1],   // 새 빛나는 Material
            originMats[1],      // 기존 Material 중 하나
            newMat,             // 기존 Material 중 하나의 Texture를 받은 빛나는 Material
            highlightMats[0]    // 새 Outline Material
        };
        rend.materials = newMats;

        // 초기에는 빛나는 것, Outline 끄기
        SetNearPlayerActive(child, false);
    }
}
