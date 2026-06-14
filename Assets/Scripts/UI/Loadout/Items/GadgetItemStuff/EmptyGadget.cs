using UnityEngine;

[CreateAssetMenu(fileName = "EmptySlot", menuName = "Heist Game/Loadout Items/Gadgets/Empty Slot")]
public class EmptyGadget : GadgetItem {
    protected override async Awaitable<bool> OnExecute() {
        return false; 
    }
}