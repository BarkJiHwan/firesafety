using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    bool isActive = false;

    private readonly int _playerLayer = 9;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == _playerLayer)
        {
            Debug.Log("플레이어는 무시함 : " + other);
            return;
        }

        // 예방일 때만 작동
        if(GameManager.Instance.CurrentPhase != GamePhase.Prevention)
        {
            return;
        }
        Debug.Log("Trigger Enter : " + other);

        var preventFire = other.GetComponent<FirePreventable>();
        if(preventFire == null)
        {
            return;
        }

        // 예외인 애들 추가
        preventFire.MakeExceptPreventObject(preventFire.MyType);
        preventFire.SetActiveOnMaterials(true);

        isActive = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.parent == gameObject.transform.parent)
        {
            return;
        }

        if (GameManager.Instance.CurrentPhase != GamePhase.Prevention)
        {
            return;
        }

        var preventFire = other.GetComponent<FirePreventable>();
        if(preventFire == null || preventFire.IsFirePreventable == true)
        {
            return;
        }

        if(isActive == false)
        {
            preventFire.MakeExceptPreventObject(preventFire.MyType);
            preventFire.SetActiveOnMaterials(true);
            isActive = true;
        }

        // 플레이어가 가까워질수록 내 Material _RimPower -시켜야 함 2->-0.2
        float distance = Vector3.Distance(other.transform.position, transform.position);

        // 빛을 더 밝게 빛나기 위해서 * 2 했음
        float t = (1 - Mathf.Clamp01(distance / 2f)) * 2;

        if(preventFire.GetHighlightProperty() == true)
        {
            preventFire.SetHighlight(t);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Prevention)
        {
            return;
        }

        Debug.Log("Trigger Exit : " + other);

        var preventFire = other.GetComponent<FirePreventable>();
        if(preventFire == null)
        {
            return;
        }

        if(isActive == true)
        {
            preventFire.SetActiveOnMaterials(false);
            // 예외인 애들 추가
            preventFire.MakeExceptObjectOff();
        }
    }

    // 에디터에서만 DrawGizmo 그려주기
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 playerPos = gameObject.transform.position;
        playerPos += new Vector3(0, 0.5f, 0);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerPos, 2);
        Handles.Label(playerPos + Vector3.up, "Interaction Collider");
    }
    #endif
}
