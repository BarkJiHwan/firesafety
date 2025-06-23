using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IInteractableInPhase2
{
    public void InteractWithXR();
}
public class Phase2ObjectManager : MonoBehaviour
{
    public static Phase2ObjectManager Instance
    {
        get; private set;
    }
    public void SupplyTowel(EHandType type)
    {

    }
    public void TurnOnWater()
    {

    }
    public void WettingTowel(EHandType type)
    {

    }
    public void FireAlarm()
    {

    }
    public void GrabWeapon(EHandType type)
    {

    }
}
