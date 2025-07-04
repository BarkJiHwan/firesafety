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
    //본래는 소화전을 누르면 무기를 집어서 보스전과 태우리 처치 용도로 사용하려 했지만
    //보스전이 사라지며 이름만 무기 집기인 상태입니다.
    public void GrabWeapon(EHandType type)
    {
        if (_player != null)
        {
            SettingPlayer();

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
