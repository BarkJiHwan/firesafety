using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Phase2_FireAlram : MonoBehaviour
{
    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log("소화전");
        if (SupplyManager.Instance != null)
        {
            Phase2ObjectManager.Instance.FireAlarm();
        }
    }
}
