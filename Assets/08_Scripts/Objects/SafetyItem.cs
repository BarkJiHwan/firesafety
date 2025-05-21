using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PreventType
{
    OldWire,
    HairDryer,
    PowerBank,
    WashingMachine,
    ElectricKettle,
    Microwave,
    Iron,
    PowerStrip,
    FluorescentLight,
    ElectricBlanket
}
[System.Serializable]
public class SafetyItem
{
    public string ID;
    public PreventType Type;
    public string Name;
    public string Location;
    public string EnglishName;
    public string Description;
}
