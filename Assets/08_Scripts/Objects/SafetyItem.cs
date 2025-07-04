using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 예방 오브젝트 이름 = 타입 연결
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
    public string ID;             // 아이템 고유 ID
    public PreventType Type;      // 예방 타입
    public string Name;           // 아이템 이름
    public string Location;       // 위치 정보
    public string EnglishName;    // 영어 이름
    public string Description;    // 설명
}
