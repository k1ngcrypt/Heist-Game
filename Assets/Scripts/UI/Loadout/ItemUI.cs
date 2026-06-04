using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    public void OnPointerDown(PointerEventData eventData)
    {
        if (myItem.name == "Empty")
        {
            return;
        }
    }

    //drag & drop
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (myItem.name == "Empty")
        {
            return;
        }
        InventoryManager.Instance.DestroyOverlay();
        InventoryManager.Instance.StartDrag(slotOrigin);
        canvasGroup.blocksRaycasts = false;
        hasDropped = false;
    }

    //drag & drop
    public void OnDrag(PointerEventData eventData)
    {
        if (myItem.name == "Empty")
        {
            return;
        }
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    //drag & drop
    public void OnEndDrag(PointerEventData eventData)
    {
        if (myItem.name == "Empty")
        {
            return;
        }
        InventoryManager.Instance.EndDrag();
        if (!hasDropped)
        {
            // Bag it
            Debug.Log("Dropped in nowhere");
            Destroy(gameObject);
            InventoryManager.Instance.ConfirmFiller();
        }
    }

    public void HasBeenDropped()
    {
        hasDropped = true;
    }

}