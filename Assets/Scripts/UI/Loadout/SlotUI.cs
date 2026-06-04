using UnityEngine;
using UnityEngine.EventSystems;

public class SlotUI : MonoBehaviour, IDropHandler
{
    public ItemType itemType;
    public int idx;
    private ItemUI currentItem;

    public void Initialize(ItemType itemType, int idx)
    {
        this.itemType = itemType;
        this.idx = idx;
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
                Debug.Log("Dropped in slot");
                draggedItem.HasBeenDropped();
                if (currentItem != null && currentItem.myItem.itemTitle != "Empty")
                {
                    draggedItem.slotOrigin.UpdateItem(currentItem);
                    currentItem.GetComponent<RectTransform>().position = draggedItem.slotOrigin.GetComponent<RectTransform>().position;
                    currentItem.UpdateSlot(draggedItem.slotOrigin);
                    InventoryManager.Instance.RemoveFiller();
                } else if (currentItem != null)
                {
                    Destroy(currentItem.gameObject);
                    currentItem = null;
                    InventoryManager.Instance.ConfirmFiller();
                }
                UpdateItem(draggedItem);
                draggedItem.GetComponent<RectTransform>().position = GetComponent<RectTransform>().position;
                draggedItem.UpdateSlot(this);
            }
        }
    }
}
