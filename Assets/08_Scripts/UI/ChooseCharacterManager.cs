using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ChooseCharacterManager : MonoBehaviour
{
    [SerializeField] CharacterManager characterMgr;
    [SerializeField] CharacterChoose charChoose;
    [SerializeField] PlayerEnum charType;
    [SerializeField] Material doNotChooseMat;

    SkinnedMeshRenderer skinnedMesh;

    public PlayerEnum myType { get; private set; }

    Material[] mats;
    Material[] changeMats;
    Texture[] originTextures;

    void Start()
    {
        var interact = GetComponentInChildren<XRBaseInteractable>();
        skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();

        // 새로운 메테리얼 생성
        MakeNewMaterial();

        //Shader shader = GetComponentInChildren<SkinnedMeshRenderer>().material.shader;
        //int propertyCount = shader.GetPropertyCount();
        //for(int i=0; i<propertyCount; i++)
        //{
        //    Debug.Log("속성 : " + shader.GetPropertyName(i));
        //}

        interact.selectEntered.AddListener(OnSelected);
        interact.selectExited.AddListener(OnDisSelected);
    }


    void OnSelected(SelectEnterEventArgs args)
    {
        SceneController.Instance.GetChooseCharacterType(charType);
        charChoose.SetActiveButton(charType);
        characterMgr.selectCharacter = gameObject;
        //skinnedMesh.materials = mats;
        characterMgr.MakeChooseCharacter();
    }

    void OnDisSelected(SelectExitEventArgs args)
    {
        //skinnedMesh.materials = changeMats;
    }

    void MakeNewMaterial()
    {
        mats = new Material[skinnedMesh.materials.Length];
        changeMats = new Material[skinnedMesh.materials.Length];
        mats = skinnedMesh.materials;
        originTextures = new Texture[mats.Length];
        for (int i = 0; i < mats.Length; i++)
        {
            changeMats[i] = new Material(doNotChooseMat);
            originTextures[i] = mats[i].GetTexture("_1st_ShadeMap");
            changeMats[i].SetTexture("_DontChoose", originTextures[i]);
        }
    }

    public void ChangeMaterialToGrey()
    {
        if(skinnedMesh.materials != changeMats)
        {
            skinnedMesh.materials = changeMats;
        }
    }

    public void ChangeOriginMaterial()
    {
        if (skinnedMesh.materials != mats)
        {
            skinnedMesh.materials = mats;
        }
    }
}
