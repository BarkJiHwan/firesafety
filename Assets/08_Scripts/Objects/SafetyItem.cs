using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PreventType
{
    ElectricBlanket,
    OldWire,
    HairDryer,
    PowerBank,
    Microwave,
    ElectricKettle,
    Iron,
    PowerStrip,
    PortableGasStove,
    Asyrtay
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
