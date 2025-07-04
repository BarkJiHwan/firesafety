using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ChooseCharacterManager : MonoBehaviour
{
    [SerializeField] CharacterManager characterMgr;
    // 캐릭터 선택 제어
    [SerializeField] CharacterChoose charChoose;
    // 해당 캐릭터 데이터
    [SerializeField] PlayerCharacterSo charInfo;
    // 안 고른 캐릭터들 표시용 Material
    [SerializeField] Material doNotChooseMat;

    SkinnedMeshRenderer skinnedMesh;

    public PlayerEnum myType { get; private set; }

    Material[] mats;
    Material[] changeMats;
    Texture[] originTextures;

    void Start()
    {
        // 상속받은 XR Interactable 컴포넌트 가져오기
        var interact = GetComponentInChildren<XRBaseInteractable>();
        // 캐릭터 모델의 SkinnedMeshRenderer 가져오기
        skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();

        // 새로운 메테리얼 생성
        MakeNewMaterial();

        //Shader shader = GetComponentInChildren<SkinnedMeshRenderer>().material.shader;
        //int propertyCount = shader.GetPropertyCount();
        //for(int i=0; i<propertyCount; i++)
        //{
        //    Debug.Log("속성 : " + shader.GetPropertyName(i));
        //}

        // XR 상호작용 이벤트 연결
        interact.selectEntered.AddListener(OnSelected);
        interact.selectExited.AddListener(OnDisSelected);
    }

    // 캐릭터가 선택되었을때
    void OnSelected(SelectEnterEventArgs args)
    {
        //SceneController.Instance.GetChooseCharacterType(charType);
        // 캐릭터 버튼 UI 활성화 (선택한 캐릭터 타입에 해당하는 버튼만 켜짐)
        charChoose.SetActiveButton(charInfo.characterType);
        // 선택된 캐릭터 등록
        characterMgr.selectCharacter = gameObject;
        //skinnedMesh.materials = mats;
        // 선택된 캐릭터와 그 외 캐릭터 후처리 실행
        characterMgr.MakeChooseCharacter();
    }

    void OnDisSelected(SelectExitEventArgs args)
    {
        //skinnedMesh.materials = changeMats;
    }

    // 회색화 Material을 위한 Material 복사 및 텍스처 설정
    void MakeNewMaterial()
    {
        mats = new Material[skinnedMesh.materials.Length];
        changeMats = new Material[skinnedMesh.materials.Length];
        mats = skinnedMesh.materials;
        originTextures = new Texture[mats.Length];
        for (int i = 0; i < mats.Length; i++)
        {
            // 새로운 Material 생성
            changeMats[i] = new Material(doNotChooseMat);
            // 기존 Material의 텍스처 복사
            originTextures[i] = mats[i].GetTexture("_1st_ShadeMap");
            // _DontChoose 슬롯에 적용
            changeMats[i].SetTexture("_DontChoose", originTextures[i]);
        }
    }

    // 회색 Material로 변경 (선택 해제 또는 비활성화 표시용)
    public void ChangeMaterialToGrey()
    {
        if(skinnedMesh.materials != changeMats)
        {
            skinnedMesh.materials = changeMats;
        }
    }

    // 원래 Material로 복구 (선택 시 다시 복원)
    public void ChangeOriginMaterial()
    {
        if (skinnedMesh.materials != mats)
        {
            skinnedMesh.materials = mats;
        }
    }
}
