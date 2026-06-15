using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private Button myButton;
    public int idx;
    public ItemUI currentItem;
    public string slotType;

    public void Initialize(int idx, LoadoutItems slotType)
    {
        this.idx = idx;
        if (slotType is GadgetItem) this.slotType = "GadgetItem";
        else this.slotType = slotType.GetType().Name;
        gameObject.name = $"Slot_{idx}";
        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        InventoryManager.Instance.SelectedItem(this);
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
