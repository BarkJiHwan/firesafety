using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IInteractableInPhase2
{
    public void InteractWithXR();
}
public class Phase2ObjectManager : MonoBehaviour
{
    [SerializeField] private ExitSobaek _sobaek;
    [SerializeField] private Phase2InteractManager _player;
    [SerializeField] ExitSupplyManager _exitSupplyMgr;
    public static Phase2ObjectManager Instance
    {
        get; private set;
    }
    private void Awake() => Instance = this;

    public void SupplyTowel(EHandType type)
    {
        SettingPlayer();
        if (_player != null)
        {
            _player.TowelSupply(type);
        }
    }
    public void WettingTowel(EHandType type)
    {
        if (_player != null && !_player.gotWet)
        {
            _player.WettingTowel(type);
            CarEnable();

            // 타월과 수도 빛나는거 끄기
            _exitSupplyMgr.SetTowelAndWater(false);
        }
    }
    public void GrabWeapon(EHandType type)
    {
        if (_player != null)
        {
            SettingPlayer();
            _player.GrabWeapon(type);

            // 소화전 빛나는거 끄기
            _exitSupplyMgr.SetFireAlarmMat(false);
        }
    }
    public void CarEnable() => _sobaek.ActivateSobaekCar();
    private void SettingPlayer()
    {
        if (_player == null)
        {
            _player = FindObjectOfType<Phase2InteractManager>();
        }
        if (_sobaek == null)
        {
            _sobaek = FindObjectOfType<ExitSobaek>();
        }
    }
}
