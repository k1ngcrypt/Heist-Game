using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "ArmourItem", menuName = "Heist Game/Loadout Items/Armour Item")]
public class ArmourItem : LoadoutItems {
    [Header("Armour Specific Item Stuff")]
    [SerializeField] public int armourValue;
    [SerializeField] public bool isDisguise = false;
    protected override void ResetItemStats() {} //Does Nothing For Armour
}
