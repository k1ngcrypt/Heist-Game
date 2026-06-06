using UnityEngine;
using UnityEngine.EventSystems;

public class SlotUI : MonoBehaviour, IDropHandler
{
    public ItemType itemType;
    public int idx;
    public ItemUI currentItem;

    public void Initialize(ItemType itemType, int idx)
    {
        this.itemType = itemType;
        this.idx = idx;
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
