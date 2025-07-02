using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phase2TowelChecking : MonoBehaviour
{
    [SerializeField] private Phase2InteractManager _interactManager;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Shield"))
        {
            if (_interactManager == null)
            {
                _interactManager = FindObjectOfType<Phase2InteractManager>();
            }
            _interactManager.CheckingTowelCol();
            enabled = false;
        }
    }
}
