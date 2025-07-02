using UnityEngine;
using UnityEngine.UI;

public class SceneInitializer : MonoBehaviour
{
    private Button _button;

    // 버튼 동적으로 이벤트 달아주기
    void Start()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(SceneController.Instance.MoveToSceneChoose);
    }
}
