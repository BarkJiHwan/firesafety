using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    List<Material> materials;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent == gameObject.transform.parent)
        {
            Debug.Log("이건 나임 : " + other);
            return;
        }

        Debug.Log("Trigger Enter : " + other);

        Renderer rend = other.GetComponent<Renderer>();
        materials = new List<Material>();
        foreach(var mat in rend.materials)
        {
            materials.Add(mat);
            mat.SetFloat("isNearPlayer", 1);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Trigger Exit : " + other);
        foreach (var mat in materials)
        {
            mat.SetFloat("isNearPlayer", 0);
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
