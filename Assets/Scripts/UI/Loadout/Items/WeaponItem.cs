using UnityEngine;

[CreateAssetMenu(fileName = "WeaponItem", menuName = "Heist Game/Loadout Items/Weapon Item")]
public class WeaponItem : LoadoutItems {
    [Header("Weapon Specific Item Stuff")]
    [SerializeField] public int damageValue;
    [SerializeField] public float soundDistance;
    [SerializeField] public float range;
    [SerializeField] public int ammoCapacity = 1;
    public int currentAmmo;

    protected override void ResetItemStats() {
        currentAmmo = ammoCapacity;
    }
    public void ReloadWeapon() { ResetItemStats(); }
}
