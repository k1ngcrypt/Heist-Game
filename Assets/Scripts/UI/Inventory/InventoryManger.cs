using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class InventoryManager : MonoBehaviour
{    
    [SerializeField] private ItemUI itemPrefab;
    [SerializeField] private SlotUI slotPrefab;
    [SerializeField] private ItemOverlay itemOverlayPrefab;
    [SerializeField] private BagUI bagPrefab;
    [SerializeField] private LoadoutItems emptyArmour;
    [SerializeField] private LoadoutItems emptyWeapon;
    [SerializeField] private LoadoutItems emptyGadget;
    
    [SerializeField] public int extraSlots = 1;

    private Transform player;
    private TurnManager turnManager;

    public int startingArmour = 1;
    public int startingWeapons = 1;
    
    private List<LoadoutItems> allItems;
    private List<LoadoutItems> startingLoadout;
    private List<SlotUI> allSlots;
    private ItemOverlay currentOverlay;
    
    private GameObject inventoryBox;
    private Transform inventorySlots;
    private Transform equippedSlots;
    private Canvas canvas;
    private RectTransform divider;
    private ItemUI fillerItem;
    private Transform topLayer;
    private SlotUI overlaySlot;
    private SlotUI equippedSlot = null;
    private Color unselected = new Color(1, 1, 1, 100f/255f);
    private Color selected = new Color(0, 1, 0, 100f/255f);

    public static InventoryManager Instance { get; private set; }

    public static BagUI currentBag;
    public static Transform bagInv;

    [Header("Set Inv (ONLY IF IT ISNT SET, FOR TESTING)")]
    [SerializeField] private List<LoadoutItems> testingLoadout;



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
        if (testingLoadout != null && testingLoadout.Count > 0)
        {
            InitializeInventory(testingLoadout);
        }
    }

    public void InitializeInventory(List<LoadoutItems> loadout)
    {
        allItems = new List<LoadoutItems>();
        for(int i = 0; i < loadout.Count; i++)
        {
            allItems.Add(GetSafeItemInstance(loadout[i]));
        }
        for(int i = 0; i < extraSlots; i++)
        {
            allItems.Add(emptyGadget);
        }
        startingLoadout = new List<LoadoutItems>();
        foreach(var item in allItems)
        {
            startingLoadout.Add(item);
        }
    }

    public void CreateInventory(Transform player, TurnManager turnManager, GameObject inventoryArea, GameObject slotsArea, GameObject equipArea, Transform div, Canvas canvas, Transform bagInv, Transform top)
    {
        allItems = new List<LoadoutItems>();
        for(int i = 0; i < startingLoadout.Count; i++)
        {
            allItems.Add(GetSafeItemInstance(startingLoadout[i]));
        }
        this.player = player;
        this.turnManager = turnManager;
        
        inventoryBox = inventoryArea;
        divider = div.GetComponent<RectTransform>();
        inventorySlots = slotsArea.transform;
        equippedSlots = equipArea.transform;
        this.canvas = canvas;
        topLayer = top;

        int countEquipped = 0;
        int countInv = 0;

        InventoryManager.bagInv = bagInv;

        allSlots = new List<SlotUI>();

        for(int idx = 0; idx < startingLoadout.Count; idx++)
        {
            LoadoutItems item = allItems[idx];

            SlotUI itemSlot;
            if (item is not GadgetItem)
            {
                itemSlot = Instantiate(slotPrefab, equippedSlots);
                countEquipped++;
            } else
            {
                itemSlot = Instantiate(slotPrefab, inventorySlots);
                countInv++;
            }
            itemSlot.Initialize(idx, item);
            allSlots.Add(itemSlot);
        }
        
        var rectInv = inventoryBox.GetComponent<RectTransform>();
        var equipRect = equippedSlots.GetComponent<RectTransform>();
        var slotRect = inventorySlots.GetComponent<RectTransform>();
        
        ResizeElement(equippedSlots, slotPrefab.GetComponent<RectTransform>().rect.width, countEquipped);
        ResizeElement(inventorySlots, slotPrefab.GetComponent<RectTransform>().rect.width, countInv);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectInv);

        rectInv.sizeDelta = new Vector2(equipRect.sizeDelta.x + divider.sizeDelta.x + slotRect.sizeDelta.x , rectInv.sizeDelta.y);

        for(int i = 0; i < startingLoadout.Count; i++)
        {
            LoadoutItems item = allItems[i];
            ItemUI itemUI = Instantiate(itemPrefab, allSlots[i].transform);
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
        Transform parent = topLayer;
        DestroyOverlay();
        currentOverlay = Instantiate(itemOverlayPrefab, parent);
        currentOverlay.Initialize(btnOrigin.myItem);
        overlaySlot = btnOrigin.slotOrigin;

        Vector3 worldPos = btnOrigin.GetComponent<RectTransform>().position;
        Vector3 localPos = parent.InverseTransformPoint(worldPos);
        float offset = 20f;
        int side = 1;
        if (overlaySlot.idx == -1)
        {
            side = -1;
        }

        currentOverlay.GetComponent<RectTransform>().localPosition = localPos - side * new Vector3(itemPrefab.GetComponent<RectTransform>().rect.width*0.5f + btnOrigin.GetComponent<RectTransform>().rect.width*0.5f + offset, 0, 0);
    }

    public void DestroyOverlay()
    {
        if (currentOverlay != null)
        {
            Destroy(currentOverlay.gameObject);
            currentOverlay = null;
            overlaySlot = null;
        }
    }

    public void StartDrag(ItemUI itm, SlotUI slot)
    {
        //create filler
        Transform parent = slot.transform;
        ItemUI itemUI = Instantiate(itemPrefab, parent);
        LoadoutItems item;
        if (slot.slotType == "ArmourItem")
        {
            item = emptyArmour;           
        } else if (slot.slotType == "WeaponItem")
        {
            item = emptyWeapon;   
        } else
        {
            item = emptyGadget;
        }
        itemUI.Initialize(item, slot, canvas);
        itemUI.GetComponent<RectTransform>().position = slot.transform.position;

        fillerItem = itemUI;

        //put item on top
        itm.transform.SetParent(topLayer);
        
        SetAllItemsRaycast(false);
    }

    public void EndDrag()
    {
        SetAllItemsRaycast(true);
    }

    private void SetAllItemsRaycast(bool value)
    {
        foreach (SlotUI slot in allSlots)
        {
            ItemUI child = slot.currentItem;
            if (child == null) continue;
            var cg = child.GetComponent<CanvasGroup>();
            if (cg != null) {
                cg.blocksRaycasts = value;
            }
        }
        if (fillerItem != null)
        {
            fillerItem.GetComponent<CanvasGroup>().blocksRaycasts = value;
        }
        if (bagInv.gameObject.activeSelf)
        {
            foreach (ItemUI child in currentBag.allItems)
            {
                if (child == null) continue;
                var cg = child.GetComponent<CanvasGroup>();
                if (cg != null) {
                    cg.blocksRaycasts = value;
                }
            }
        }
    }

    private void RemoveFiller()
    {
        if (fillerItem != null)
        {
            Destroy(fillerItem.gameObject);
            fillerItem = null;
        }
    }

    private void ConfirmFiller()
    {
        if (fillerItem != null)
        {
            fillerItem.slotOrigin.UpdateItem(fillerItem);
            if (fillerItem.slotOrigin.idx == -1)
            {
                fillerItem.transform.SetParent(fillerItem.slotOrigin.transform, true);
            }
        }
    }

    public void SwapItems(ItemUI newItm, ItemUI oldItm) //note newItm is the item being dragged into the slot, oldItm is the item in that slot
    {
        SlotUI newSlot = null;
        SlotUI oldSlot = null;
        if (newItm != null)
        {
            oldSlot = newItm.slotOrigin; //its old slot as its where the new item is comming from
        } else
        {
            return;
        }
        if (oldItm != null)
        {
            newSlot = oldItm.slotOrigin; //its new slot as its where new item is going to
        } else
        {
            return;
        }

        //check to see if item can go in that slot
        bool isNewItemMatch = newSlot.slotType == "GadgetItem" || (newItm.myItem.GetType().Name == newSlot.slotType);
        if (!isNewItemMatch)
        {
            NotificationManager.Instance.SendNotification($"Can't move {newItm.myItem.GetType().Name} item to {newSlot.slotType} slot");
            ReturnItem(newItm);
            return;
        }
        bool isOldItemMatch = oldItm.myItem.itemTitle == "Empty" || oldSlot.slotType == "GadgetItem" || (oldItm.myItem.GetType().Name == oldSlot.slotType);
        if (!isOldItemMatch)
        {
            NotificationManager.Instance.SendNotification($"Can't move {oldItm.myItem.GetType().Name} item to {oldSlot.slotType} slot");
            ReturnItem(newItm);
            return;
        }

        if (oldSlot == newSlot)
        {
            ReturnItem(newItm);
            return;
        }

        CheckEquipped(oldSlot);
        CheckEquipped(newSlot);

        //swap items visually
        if (oldItm != null && oldItm.myItem.itemTitle != "Empty")
        {
            oldSlot.UpdateItem(oldItm);
            oldItm.GetComponent<RectTransform>().position = oldSlot.GetComponent<RectTransform>().position;
            oldItm.UpdateSlot(oldSlot);
            RemoveFiller();
        } else if (oldItm != null)
        {
            //empty, instead of swap, destroy current, and replace with filler
            Destroy(oldItm.gameObject);
            oldItm = null;
            ConfirmFiller();
        }
        newSlot.UpdateItem(newItm);
        newItm.GetComponent<RectTransform>().position = newSlot.GetComponent<RectTransform>().position;
        newItm.UpdateSlot(newSlot);

        //adjust loadout, and for safety, the bag updates are at the end
        newItm.transform.SetParent(newSlot.transform, true);
        (oldItm ?? fillerItem).transform.SetParent(oldSlot.transform, true);
        if (newSlot.idx == -1 && oldSlot.idx == -1) {
            //swaps two in bag. New done first to not destroy bag
            if (oldItm == null)
            {
                currentBag.ReplaceFromSlot(fillerItem, newSlot); //put it where the original was
            }
            currentBag.SwapItemsInList(newItm, oldItm ?? fillerItem);
        }
        else if (oldSlot.idx == -1 && newSlot.idx != -1)
        {
            //swap adds new to inventory
            allItems[newSlot.idx] = newItm.myItem;
            currentBag.ReplaceFromSlot(oldItm ?? fillerItem, oldSlot);
        }
        else if (newSlot.idx == -1 && oldSlot.idx != -1)
        {
            //swap removes new from inv
            allItems[oldSlot.idx] = (oldItm ?? fillerItem).myItem;
            currentBag.ReplaceFromSlot(newItm, newSlot);
        } else
        {
            //swap two items in the inventory
            allItems[newSlot.idx] = newItm.myItem;
            allItems[oldSlot.idx] = (oldItm ?? fillerItem).myItem;
        }
        fillerItem = null;
    }

    public void ReturnItem(ItemUI rtnItm)
    {
        rtnItm.GetComponent<RectTransform>().position = rtnItm.slotOrigin.GetComponent<RectTransform>().position;
        rtnItm.transform.SetParent(rtnItm.slotOrigin.GetComponent<Transform>());
        RemoveFiller();
    }

    public void BagItem(ItemUI item)
    {
        BagItem(item, player.position);
    }

    public void BagItem(ItemUI item, Vector2 position)
    {
        if (item.slotOrigin.idx == -1)
        {
            //alr in bag, just return it
            ReturnItem(item);
            return;
        }

        CheckEquipped(item.slotOrigin);

        if (currentBag == null)
        {
            //no bag, create new one
            BagUI bag = Instantiate(bagPrefab);
            bag.Initialize(position, turnManager, canvas, itemPrefab, slotPrefab, emptyGadget);
            currentBag = bag;
            AwarenessManager.Instance.RegisterAnomaly(bag.transform);
        }
        allItems[item.slotOrigin.idx] = fillerItem.myItem;
        ConfirmFiller();
        fillerItem = null;
        currentBag.DroppedItem(item);
    }

    public void CheckHeldItem()
    {
        foreach (Transform child in topLayer)
        {
            if (child.GetComponent<ItemUI>() != null)
            {
                ItemUI item = child.GetComponent<ItemUI>();
                if (item != null)
                {
                    if (item.slotOrigin.idx == -1)
                    {
                        EndDrag();
                        Destroy(item.gameObject);
                    }
                }
            }
        }
        if (overlaySlot != null)
        {
            if (overlaySlot.idx == -1)
            {
                DestroyOverlay();
            }
        }
    }

    //API for developer interaction
    public bool HasItem(LoadoutItems item)
    {
        foreach(LoadoutItems check in allItems)
        {
            if (check == item)
            {
                return true;
            }
        }
        return false;
    }

    public LoadoutItems ReturnEquipArmour()
    {
        if (allItems == null||allSlots == null) return emptyArmour;
        for(int i = 0; i < allSlots.Count; i++)
        {
            SlotUI slot = allSlots[i];
            if (slot.slotType == "ArmourItem")
            {
                return allItems[i];
            }
        }
        return emptyArmour;
    }
    public LoadoutItems ReturnEquipWeapon()
    {
        SlotUI slot;
        for(int i = 0; i < allSlots.Count; i++)
        {
            slot = allSlots[i];
            if (slot.slotType == "WeaponItem")
            {
                return allItems[i];
            }
        }
        return emptyWeapon;
    }

    public LoadoutItems ReturnEquipItem()
    {
        if (equippedSlot == null)
        {
            return emptyGadget;
        }
        return equippedSlot.currentItem.myItem;
    }

    public bool TryAddItem(LoadoutItems item)
    {
        LoadoutItems check = emptyGadget;
        for(int i = 0; i < allItems.Count; i++)
        {
            check = allItems[i];
            bool typeMatch = check.GetType() == item.GetType() || (check is GadgetItem && item is GadgetItem);
            if (check.itemTitle == "Empty" && typeMatch)
            {
                SlotUI slot = allSlots[i];
                ItemUI oldItem = slot.currentItem;
                ItemUI newItem = Instantiate(itemPrefab, allSlots[i].transform);
                newItem.Initialize(GetSafeItemInstance(item), slot, canvas);
                newItem.GetComponent<Transform>().position = slot.GetComponent<Transform>().position;

                slot.UpdateItem(newItem);

                allItems[i] = item;

                Destroy(oldItem.gameObject);
                
                return true;
            }
        }
        return false;
    }

    public bool TryRemoveItem(LoadoutItems item)
    {
        LoadoutItems check = emptyGadget;
        for(int i = 0; i < allItems.Count; i++)
        {
            check = allItems[i];
            if (check == item)
            {
                SlotUI slot = allSlots[i];
                ItemUI oldItem = slot.currentItem;
                ItemUI replaceEmpty = Instantiate(itemPrefab, allSlots[i].transform);
                LoadoutItems empty;
                if (item is ArmourItem) empty = emptyArmour;
                else if (item is WeaponItem) empty = emptyWeapon;
                else empty = emptyGadget;
                replaceEmpty.Initialize(empty, slot, canvas);
                replaceEmpty.GetComponent<Transform>().position = slot.GetComponent<Transform>().position;

                slot.UpdateItem(replaceEmpty);

                allItems[i] = empty;

                Destroy(oldItem.gameObject);
                
                CheckEquipped(slot);

                return true;
            }
        }
        return false;
    }

    public void SelectedItem(SlotUI slot)
    {
        if (slot == null || slot.idx == -1 || slot.currentItem.myItem.itemTitle == "Empty" || slot.currentItem.myItem is not GadgetItem) return;

        if (equippedSlot == slot)
        {
            slot.GetComponent<Image>().color = unselected;
            equippedSlot = null;
            return;
        } else if (equippedSlot != null)
        {
            equippedSlot.GetComponent<Image>().color = unselected;
        }
        equippedSlot = slot;
        slot.GetComponent<Image>().color = selected;
    }

    private void CheckEquipped(SlotUI slot)
    {
        if (equippedSlot == null || slot == null || equippedSlot != slot) return;
        equippedSlot.GetComponent<Image>().color = unselected;
        equippedSlot = null;
    }

    private LoadoutItems GetSafeItemInstance(LoadoutItems sourceItem) {
        if (sourceItem == null) return null;
        if (sourceItem.itemTitle == "Empty") return sourceItem; 
        return Instantiate(sourceItem);
    }

    public void UpdateVisual(LoadoutItems item)
    {
        for(int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i] == item)
            {
                allSlots[i].currentItem.UpdateVisual();
            }
        }
    }

    public void InitializeAllItems() {
        for (int i = 0; i < allItems.Count; i++) allItems[i].ResetItemStats();
    }

    public void DropItem(LoadoutItems item, Vector2 position)
    {
        if (item == null || item.itemTitle == "Empty") return;

        BagUI bag = Instantiate(bagPrefab);
        bag.Initialize(position, turnManager, canvas, itemPrefab, slotPrefab, emptyGadget);
        AwarenessManager.Instance.RegisterAnomaly(bag.transform);

        // Build a temporary ItemUI so DroppedItem can consume it
        SlotUI tempSlot = Instantiate(slotPrefab, bag.transform);
        tempSlot.Initialize(-1, item);

        ItemUI tempItem = Instantiate(itemPrefab, tempSlot.transform);
        tempItem.Initialize(GetSafeItemInstance(item), tempSlot, canvas);
        tempSlot.UpdateItem(tempItem);

        bag.DroppedItem(tempItem);
    }

    /*
    public void DebugLoadout()
    {
        string str = "Player Inv: ";
        for (int i = 0; i < allItems.Count; i++)
        {
            if(allItems[i] == null)
            {
                str += "null, ";
            } else
            {
               str += allItems[i].itemTitle + ", "; 
            }
            
        }

        str += " | Starting Loadout: ";
        for (int i = 0; i < startingLoadout.Count; i++)
        {
            if(startingLoadout[i] == null)
            {
                str += "null, ";
            } else
            {
               str += startingLoadout[i].itemTitle + ", "; 
            }
            
        }
        Debug.Log(str);
    }
    */

}