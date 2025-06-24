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
        }
    }
    public void GrabWeapon(EHandType type)
    {
        if (_player != null)
        {
            SettingPlayer();
            _player.GrabWeapon(type);
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
