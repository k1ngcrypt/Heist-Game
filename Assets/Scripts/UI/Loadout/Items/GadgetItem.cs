using UnityEngine;

[CreateAssetMenu(fileName = "GadgetItem", menuName = "Heist Game/Loadout Items/Gadget Item")]
public class GadgetItem : LoadoutItems {
    [Header("Gadget Specific Item Stuff")]
    [SerializeField] public int durability;
    [SerializeField] public string itemUsedFor;
}
