using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryBox : MonoBehaviour, IDropHandler
{
    [SerializeField] private GameObject inventoryArea;
    [SerializeField] private GameObject spawnArea;
    [SerializeField] private GameObject slotsArea;
    [SerializeField] private GameObject equipArea;
    [SerializeField] private Transform divider;
    [SerializeField] private Canvas canvas;

    void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.CreateInventory(inventoryArea, spawnArea, slotsArea, equipArea, divider, canvas);
        } else
        {
            Debug.LogWarning("InventoryManager instance not found. Please ensure an InventoryManager is present in the scene.", this);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            ItemUI draggedItem = eventData.pointerDrag.GetComponent<ItemUI>();
            if (draggedItem != null)
            {
                // let go in inv
                Debug.Log("Dropped in inventory box");
                draggedItem.HasBeenDropped();
                draggedItem.GetComponent<RectTransform>().position = draggedItem.slotOrigin.GetComponent<RectTransform>().position;
                InventoryManager.Instance.RemoveFiller();
            }
        }
    }
}
