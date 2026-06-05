using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImage;

    public LoadoutItems myItem;

    public SlotUI slotOrigin;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private bool hasDropped = false;

    public void Initialize(LoadoutItems item, SlotUI origin, Canvas canvas)
    {
        myItem = item;
        slotOrigin = origin;
        this.canvas = canvas;

        if (iconImage != null)
        {
            iconImage.sprite = myItem.itemIcon;
        }

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        gameObject.name = $"Item_{item.itemTitle}";
    }

    public void UpdateSlot(SlotUI newSlot)
    {
        slotOrigin = newSlot;
    }

    //overlay
    public void OnPointerEnter(PointerEventData eventData)
    {
        InventoryManager.Instance.CreateOverlay(this);
    }

    //overlay
    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryManager.Instance.DestroyOverlay();
    }

    //drag & drop
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (myItem.itemTitle == "Empty")
        {
            return;
        }
        canvasGroup.blocksRaycasts = false;
        hasDropped = false;
        InventoryManager.Instance.DestroyOverlay();
        InventoryManager.Instance.StartDrag(slotOrigin);
        
    }

    //drag & drop
    public void OnDrag(PointerEventData eventData)
    {
        if (myItem.itemTitle == "Empty")
        {
            return;
        }
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    //drag & drop
    public void OnEndDrag(PointerEventData eventData)
    {
        if (myItem.itemTitle == "Empty")
        {
            return;
        }
        InventoryManager.Instance.EndDrag();
        if (!hasDropped)
        {
            // Bag it
            InventoryManager.Instance.BagItem(this);
        }
        canvasGroup.blocksRaycasts = true;
    }

    public void HasBeenDropped()
    {
        hasDropped = true;
    }

}