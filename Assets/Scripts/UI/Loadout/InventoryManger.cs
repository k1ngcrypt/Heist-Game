using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class InventoryManager : MonoBehaviour
{    
    [SerializeField] private ItemUI itemPrefab;
    [SerializeField] private SlotUI slotPrefab;
    [SerializeField] private ItemOverlay itemOverlayPrefab;
    [SerializeField] private LoadoutItems emptyArmour;
    [SerializeField] private LoadoutItems emptyWeapon;
    [SerializeField] private LoadoutItems emptyGadget;
    
    [SerializeField] public int extraSlots = 1;

    public int startingArmour = 1;
    public int startingWeapons = 1;
    
    private List<LoadoutItems> allItems;
    private List<SlotUI> allSlots;
    private ItemOverlay currentOverlay;
    
    private Transform spawnArea;
    private GameObject inventoryBox;
    private Transform inventorySlots;
    private Transform equippedSlots;
    private Canvas canvas;
    private RectTransform divider;
    private ItemUI fillerItem;

    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        allItems = new List<LoadoutItems>{ emptyArmour, emptyWeapon };
        for(int i = 0; i < extraSlots; i++)
        {
            allItems.Add(emptyGadget);
        }
    }

    public void InitializeInventory(List<LoadoutItems> loadout)
    {
        allItems = new List<LoadoutItems>();
        for(int i = 0; i < loadout.Count; i++)
        {
            allItems.Add(loadout[i]);
        }
        for(int i = 0; i < extraSlots; i++)
        {
            allItems.Add(emptyGadget);
        }
    }

    public void CreateInventory(GameObject inventoryArea, GameObject spawnArea, GameObject slotsArea, GameObject equipArea, Transform div, Canvas canvas)
    {
        this.spawnArea = spawnArea.transform;
        inventoryBox = inventoryArea;
        divider = div.GetComponent<RectTransform>();
        inventorySlots = slotsArea.transform;
        equippedSlots = equipArea.transform;
        this.canvas = canvas;

        int countEquipped = 0;
        int countInv = 0;

        allSlots = new List<SlotUI>();

        for(int idx = 0; idx < allItems.Count; idx++)
        {
            LoadoutItems item = allItems[idx];
            SlotUI itemSlot;
            if (item.itemType != ItemType.Gadget)
            {
                itemSlot = Instantiate(slotPrefab, equippedSlots);
                countEquipped++;
            } else
            {
                itemSlot = Instantiate(slotPrefab, inventorySlots);
                countInv++;
            }
            itemSlot.Initialize(item.itemType, idx);
            allSlots.Add(itemSlot);
        }
        
        var rectInv = inventoryBox.GetComponent<RectTransform>();
        var equipRect = equippedSlots.GetComponent<RectTransform>();
        var slotRect = inventorySlots.GetComponent<RectTransform>();
        
        ResizeElement(equippedSlots, slotPrefab.GetComponent<RectTransform>().rect.width, countEquipped);
        ResizeElement(inventorySlots, slotPrefab.GetComponent<RectTransform>().rect.width, countInv);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectInv);

        rectInv.sizeDelta = new Vector2(equipRect.sizeDelta.x + divider.sizeDelta.x + slotRect.sizeDelta.x , rectInv.sizeDelta.y);

        for(int i = 0; i < allItems.Count; i++)
        {
            LoadoutItems item = allItems[i];
            ItemUI itemUI = Instantiate(itemPrefab, this.spawnArea);
            itemUI.Initialize(item, allSlots[i], canvas);
            itemUI.GetComponent<RectTransform>().position = allSlots[i].transform.position;
            allSlots[i].UpdateItem(itemUI);
        }
    }

    private void ResizeElement(Transform element, float width, int count)
    {
        var layout = element.GetComponent<GridLayoutGroup>();
        var rect = element.GetComponent<RectTransform>();
    
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        
        float spacing = layout.spacing.x;
        float rightPad = layout.padding.right;
        float leftPad = layout.padding.left;

        if (count % 2 == 1)
        {
            count = (count + 1) / 2;
        } else
        {
            count /= 2;
        }

        rect.sizeDelta = new Vector2(((spacing + width) * count) + leftPad + rightPad - spacing, rect.sizeDelta.y);
    }

    public void CreateOverlay(ItemUI btnOrigin)
    {
        DestroyOverlay();
        currentOverlay = Instantiate(itemOverlayPrefab, spawnArea);
        currentOverlay.Initialize(btnOrigin.myItem);

        Vector3 worldPos = btnOrigin.GetComponent<RectTransform>().position;
        Vector3 localPos = spawnArea.InverseTransformPoint(worldPos);
        float offset = 20f;

        currentOverlay.GetComponent<RectTransform>().localPosition = localPos - new Vector3(itemPrefab.GetComponent<RectTransform>().rect.width*0.5f + btnOrigin.GetComponent<RectTransform>().rect.width*0.5f + offset, 0, 0);
    }

    public void DestroyOverlay()
    {
        if (currentOverlay != null)
        {
            Destroy(currentOverlay.gameObject);
            currentOverlay = null;
        }
    }

    public void StartDrag(SlotUI slot)
    {
        SetAllItemsRaycast(false);
        
        ItemUI itemUI = Instantiate(itemPrefab, this.spawnArea);
        LoadoutItems item;
        if (slot.itemType == ItemType.Armour)
        {
            item = emptyArmour;           
        } else if (slot.itemType == ItemType.Weapon)
        {
            item = emptyWeapon;   
        } else
        {
            item = emptyGadget;
        }
        itemUI.Initialize(item, slot, canvas);
        itemUI.GetComponent<RectTransform>().position = slot.transform.position;

        fillerItem = itemUI;
    }

    public void EndDrag()
    {
        SetAllItemsRaycast(true);
    }

    private void SetAllItemsRaycast(bool value)
    {
        foreach (Transform child in spawnArea)
        {
            var cg = child.GetComponent<CanvasGroup>();
            if (cg != null) {
                cg.blocksRaycasts = value;
            }
        }
    }

    public void RemoveFiller()
    {
        if (fillerItem != null)
        {
            Destroy(fillerItem.gameObject);
            fillerItem = null;
        }
    }

    public void ConfirmFiller()
    {
        if (fillerItem != null)
        {
            fillerItem.slotOrigin.UpdateItem(fillerItem);
        }
    }

    public void AddItem(ItemUI item, int idx)
    {
        allItems[idx] = item.myItem;
    }

    public void RemoveItem(int idx)
    {
        LoadoutItems emptyItem;
        if (idx >= startingArmour + startingWeapons)
        {
            emptyItem = emptyGadget;
        } else if (allItems[idx].itemType == ItemType.Armour)
        {
            emptyItem = emptyArmour;           
        } else if (allItems[idx].itemType == ItemType.Weapon)
        {
            emptyItem = emptyWeapon;   
        } else
        {
            emptyItem = emptyGadget;
        }
        allItems[idx] = emptyItem;
    }

    public void SwapItems(int idx1, int idx2)
    {
        var temp = allItems[idx1];
        allItems[idx1] = allItems[idx2];
        allItems[idx2] = temp;    
    }
}
