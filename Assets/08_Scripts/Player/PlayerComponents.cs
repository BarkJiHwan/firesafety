using UnityEngine;

/*
 * 플레이어의 컴포넌트 찾을 수 있는 모음집, 스크립트 직접 추가 하지 말아주세여..
 * GameObject의 컴포넌트 Find를 하지 않고 저장해서 쓰기 위해 데이터만 담아두는 정보입니다.
 */
public class PlayerComponents : MonoBehaviour
{
    public GameObject xRComponents;
    public GameObject model;
    public CustomTunnelingVignette customTunnelingVignette;
    public ExitDialogue exitDialogue;
}
