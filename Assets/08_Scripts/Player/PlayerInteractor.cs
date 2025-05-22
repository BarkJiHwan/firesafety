using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent == gameObject.transform.parent)
        {
            Debug.Log("이건 나임 : " + other);
            return;
        }

        // 예방일 때만 작동
        if(GameManager.Instance.CurrentPhase != GamePhase.Prevention)
        {
            return;
        }
        Debug.Log("Trigger Enter : " + other);

        var preventFire = other.GetComponent<FirePreventable>();
        preventFire.SetActiveOnMaterials(true);
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

        Debug.Log("Trigger Stay : " + other);

        // 플레이어가 가까워질수록 내 Material _RimPower -시켜야 함 2->-0.2
        float distance = Vector3.Distance(other.transform.position, transform.position);

        float t = 1 - Mathf.Clamp01(distance / 2f);

        var preventFire = other.GetComponent<FirePreventable>();
        preventFire.SetHighlightStronger(t);
    }

    private void OnTriggerExit(Collider other)
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Prevention)
        {
            return;
        }

        Debug.Log("Trigger Exit : " + other);

        var preventFire = other.GetComponent<FirePreventable>();
        preventFire.SetActiveOnMaterials(false);
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
