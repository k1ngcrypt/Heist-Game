using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject durabilityArea;
    [SerializeField] private RectTransform currentyBar;
    [SerializeField] private RectTransform totalBar;
    [SerializeField] private GameObject ammoArea;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private RectTransform ammoImage;
    [SerializeField] private GameObject armourArea;
    [SerializeField] private TextMeshProUGUI armourText;
    [SerializeField] private RectTransform armourImage;

    public LoadoutItems myItem;

    public SlotUI slotOrigin;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private bool hasDurability = false;
    private bool hasAmmo = false;
    private bool hasArmour = false;

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

        if (item is GadgetItem && item.itemTitle != "Empty")
        {
            hasDurability = true;
            
        } else if (item is WeaponItem && item.itemTitle != "Empty")
        {
            hasAmmo = true;
        } else if (item is ArmourItem && item.itemTitle != "Empty")
        {
            hasArmour = true;
            ArmourItem itm = (ArmourItem)myItem;
            armourText.text = $"{itm.armourValue}";
            armourImage.anchoredPosition = new Vector2(-(5.5f + armourText.preferredWidth), armourImage.anchoredPosition.y); //5.5f is padding
            armourArea.GetComponent<RectTransform>().sizeDelta = new Vector2(-armourImage.anchoredPosition.x + 5.5f, armourArea.GetComponent<RectTransform>().sizeDelta.y);
        }
        UpdateVisual();
        durabilityArea.SetActive(hasDurability);
        ammoArea.SetActive(hasAmmo);
        armourArea.SetActive(hasArmour);
    }

    public void UpdateVisual()
    {
        if (hasDurability)
        {
            GadgetItem item = (GadgetItem)myItem;
            float percent = Mathf.Clamp(item.currentDurability / (1f * item.maxDurability),0f,1f);

            currentyBar.sizeDelta = new Vector2(totalBar.sizeDelta.x * percent, currentyBar.sizeDelta.y);
        } else if (hasAmmo)
        {
            WeaponItem item = (WeaponItem)myItem;
            ammoText.text = $"{item.currentAmmo}/{item.ammoCapacity}";
            ammoImage.anchoredPosition = new Vector2(-(4.5f + ammoText.preferredWidth), ammoImage.anchoredPosition.y); //4.5f is padding
            ammoArea.GetComponent<RectTransform>().sizeDelta = new Vector2(-ammoImage.anchoredPosition.x + 4.5f, ammoArea.GetComponent<RectTransform>().sizeDelta.y);
        } //hasArmour doesn't need to update as it doesnt change
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
            eventData.pointerDrag = null;
            return;
        }
        canvasGroup.blocksRaycasts = false;
        hasDropped = false;
        InventoryManager.Instance.DestroyOverlay();
        InventoryManager.Instance.StartDrag(this, slotOrigin);
        
    }

    //drag & drop
    public void OnDrag(PointerEventData eventData)
    {
        if (myItem.itemTitle == "Empty")
        {
            eventData.pointerDrag = null;
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