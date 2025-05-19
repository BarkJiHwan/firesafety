using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PreventTpye
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
public class PreventTextEntry
{
    public PreventTpye type;
    [TextArea] public string text;
}
[CreateAssetMenu(fileName = "PreventableObjData")]
public class PreventableObjData : ScriptableObject
{
    public List<PreventTextEntry> entries;
    public string GetText(PreventTpye type)
    {
        var entry = entries.Find(e => e.type == type);
        return entry != null ? entry.text : "";
    }
}
