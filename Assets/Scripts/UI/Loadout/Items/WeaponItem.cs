using UnityEngine;

[CreateAssetMenu(fileName = "WeaponItem", menuName = "Heist Game/Loadout Items/Weapon Item")]
public class WeaponItem : LoadoutItems {
    [Header("Weapon Specific Item Stuff")]
    [SerializeField] public int damageValue;
    [SerializeField] public bool isSilenced;
    [SerializeField] public bool isAmmoPowered;
    [SerializeField] public int ammoCapacity;
    public int currentAmmo;
}
