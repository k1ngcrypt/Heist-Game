using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryBox : MonoBehaviour, IDropHandler
{
    [SerializeField] private GameObject inventoryArea;
    [SerializeField] private GameObject spawnArea;
    [SerializeField] private GameObject slotsArea;
    [SerializeField] private GameObject equipArea;
    [SerializeField] private Transform divider;
    [SerializeField] private Transform bagInv;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Transform player;
    [SerializeField] private TurnManager turnManager;

    void Start()
    {
        bagInv.gameObject.SetActive(false);
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.CreateInventory(player, turnManager, inventoryArea, spawnArea, slotsArea, equipArea, divider, canvas, bagInv);
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
                // drop in inv
                draggedItem.HasBeenDropped();
                InventoryManager.Instance.ReturnItem(draggedItem);
            }
        }
    }
}
