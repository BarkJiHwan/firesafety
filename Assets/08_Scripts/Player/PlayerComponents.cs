using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/* 플레이어의 컴포넌트 찾을 수 있는 모음집, 스크립트 직접 추가 하지 말아주세여..*/
public class PlayerComponents : MonoBehaviour
{
    public GameObject xRComponents;
    public GameObject model;
    public CustomTunnelingVignette customTunnelingVignette;

    private void Start()
    {
        // StartCoroutine(TestFadeInOut());;
    }

    /* 페이드인 / 페이드 아웃 테스트용 */
    private IEnumerator TestFadeInOut()
    {
        yield return new WaitForSeconds(1f);
        customTunnelingVignette.FadeOut();
        yield return new WaitForSeconds(1f);
        customTunnelingVignette.FadeIn();
    }
}
