using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterChoose : MonoBehaviour
{
    [SerializeField] FirebaseManager firebaseMgr;
    [SerializeField] List<ButtonInteractor> checkButtonList;
    [SerializeField] GameObject xrRigObject;

    [System.Serializable]
    class ButtonInteractor
    {
        // 캐릭터 데이터
        public PlayerCharacterSo charInfo;
        // 선택 버튼
        public Button applyButton;
        // 해당 캐릭터 오브젝트
        public GameObject characterObject;
    }

    private void Start()
    {
        // 캐릭터 선택 버튼에 이벤트 리스너 등록
        foreach(var button in checkButtonList)
        {
            button.applyButton.onClick.AddListener(() =>
            {
                // XR 시점에서 페이드 아웃 비주얼 효과
                xrRigObject.GetComponent<CustomTunnelingVignette>().FadeOut();
                // 선택한 캐릭터 정보 SceneController 등록
                SceneController.Instance.SetChooseCharacterType(button.charInfo);
                Debug.Log("캐릭터 이름 : " + SceneController.Instance.GetChooseCharacterType().characterName);
                // Firebase에 캐릭터 선택 결과 저장
                firebaseMgr.SaveCharacterSelection(SceneController.Instance.GetChooseCharacterType().characterName);
                // 선택된 씬으로 이동
                MoveScene(SceneController.Instance.chooseSceneType);
            });
            // 처음엔 버튼 비활성화
            button.applyButton.gameObject.SetActive(false);
        }
        // 각자의 checkButtonList 버튼의 작동 로직 필요 => 이전의 씬에 씬 선택을 고른거에 따라 작동이 달라져야 함
        // 옵저버 패턴으로 만약에 버튼이 켜지고 확인 버튼을 누르면 이전의 씬 선택에 따라 다른 씬으로 이동
    }

    // 뒤로 가기 버튼 눌렀을 때 씬 선택 창으로 이동
    public void BackToBeforeScene()
    {
        SceneController.Instance.MoveToSceneChoose();
    }

    // 특정 타입 캐릭터만 버튼 비활성화
    public void SetActiveButton(PlayerEnum type)
    {
        foreach (var button in checkButtonList)
        {
            button.applyButton.gameObject.SetActive(button.charInfo.characterType == type);
        }
    }

    // 모든 캐릭터 오브젝트 반환
    public GameObject[] GetCharacterObject()
    {
        GameObject[] obj = new GameObject[checkButtonList.Count];
        int count = 0;
        for(int i=0; i<checkButtonList.Count; i++)
        {
            obj[i] = checkButtonList[i].characterObject;
        }
        return obj;
    }

    // 이전에 씬 선택 창에서 고른 씬으로 이동
    void MoveScene(SceneType type)
    {
        switch (type)
        {
            case SceneType.IngameScene_Fire:
                Debug.Log("IngameScene_Fire");
                SceneController.Instance.MoveToPreventionFireScene();
                break;
            case SceneType.IngameScene_Evacuation:
                Debug.Log("IngameScene_Evacuation");
                SceneController.Instance.MoveToEvacuationScene();
                break;
        }
    }
}
