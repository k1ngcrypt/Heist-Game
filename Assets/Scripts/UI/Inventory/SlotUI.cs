using UnityEngine;
using UnityEngine.EventSystems;

public class SlotUI : MonoBehaviour, IDropHandler
{
    public int idx;
    public ItemUI currentItem;
    public string slotType;

    public void Initialize(int idx, LoadoutItems slotType)
    {
        this.idx = idx;
        if (slotType is GadgetItem) this.slotType = "GadgetItem";
        else this.slotType = slotType.GetType().Name;
        gameObject.name = $"Slot_{idx}";
    }

    public void UpdateItem(ItemUI item)
    {
        currentItem = item;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            ItemUI draggedItem = eventData.pointerDrag.GetComponent<ItemUI>();
            if (draggedItem != null)
            {
                // Dropped into a slot
                draggedItem.HasBeenDropped();
                InventoryManager.Instance.SwapItems(draggedItem, currentItem);
            }
        }
    }
}
