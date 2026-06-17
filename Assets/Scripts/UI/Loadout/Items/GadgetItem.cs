using UnityEngine;

public abstract class GadgetItem : LoadoutItems {
    [Header("Gadget Specific Item Stuff")]
    [SerializeField] public int maxDurability;
    public int currentDurability;
    protected Transform player;

    private void OnValidate() {
        currentDurability = maxDurability;
        FetchPlayerReference();
    }

    private void FetchPlayerReference() {
        if (Map.Player != null) player = Map.Player.transform;
        
        if (player == null) {
            PlayerController foundPlayer = FindAnyObjectByType<PlayerController>();
            if (foundPlayer != null) player = foundPlayer.transform;
        }
    }

    public async Awaitable TryExecute() {
        bool success = await OnExecute();
        if (success) {
            currentDurability--;
            InventoryManager.Instance.UpdateVisual(this);
            if (currentDurability == 0) InventoryManager.Instance.TryRemoveItem(this);
        }
    }
    protected abstract Awaitable<bool> OnExecute();
}
