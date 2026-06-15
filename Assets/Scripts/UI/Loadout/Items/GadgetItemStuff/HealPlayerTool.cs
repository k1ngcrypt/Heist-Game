using UnityEngine;

[CreateAssetMenu(fileName = "HealTool", menuName = "Heist Game/Loadout Items/Gadgets/Heal Player Tool")]
public class HealPlayerTool : GadgetItem {
    [SerializeField] public float healAmount;
    protected override async Awaitable<bool> OnExecute() {
        if (player == null) return false;
        PlayerStats playerStat = player.GetComponent<PlayerStats>();
        if (playerStat == null) return false;
        playerStat.Heal(healAmount);
        await TurnManager.Instance.ProcessTicks(1);
        return true;
    }
}
