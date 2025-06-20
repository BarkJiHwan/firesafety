using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phase2TowelChecking : MonoBehaviour
{
    [SerializeField] private Phase2InteractManager _interactManager;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Towel"))
        {
            _interactManager.CheckingTowelCol();
            enabled = false;
        }
    }
}
