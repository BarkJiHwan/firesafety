using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerSyncronizer : MonoBehaviour
{
    public GameObject syncTarget;

    private void FixedUpdate()
    {
        syncTarget.transform.position = transform.position;
        syncTarget.transform.rotation = transform.rotation;
    }
}
