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

        Debug.Log("Trigger Enter : " + other);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Trigger Exit : " + other);
    }

    private void OnDrawGizmos()
    {
        Vector3 playerPos = gameObject.transform.position;
        playerPos += new Vector3(0, 0.5f, 0);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerPos, 2);
        Handles.Label(playerPos + Vector3.up, "Interaction Collider");
    }
}
