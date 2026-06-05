using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class BagUI : MonoBehaviour, ITurnActor
{
    private TurnManager turnManager;
    private Transform player;
    private Canvas canvas;
    
    public int TickDebt { get; set; }
    private Vector3 worldPosition;
    private Vector3 topRight;
    private Vector3 bottomLeft;
    private Vector3 buffer = new Vector3(0.25f, 0.25f, 0); //incase to make sure it will still call player

    private List<ItemUI> bagItems;
    public List<ItemUI> allItems;

    private ItemUI itemPrefab;
    private SlotUI slotPrefab;
    private LoadoutItems emptyItem;

    private Vector2 baseSize = new Vector2(275, 80);

    public void Initialize(Vector3 position, Transform playerTr, TurnManager turnM, Canvas canvas, ItemUI itemPrefab, SlotUI slotPrefab, LoadoutItems emptyItem)
    {
        turnManager = turnM;
        player = playerTr;
        GetComponent<RectTransform>().position = position;
        this.canvas = canvas;
        worldPosition = position;
        topRight = worldPosition + buffer;
        bottomLeft = worldPosition - buffer;

        turnManager.Register(this);

        bagItems = new List<ItemUI>();
        allItems = new List<ItemUI>();

        this.itemPrefab = itemPrefab;
        this.slotPrefab = slotPrefab;
        this.emptyItem = emptyItem;
    }

    public async Awaitable OnTick()
    {
        TickDebt = 0;
        Vector3 playerPosition = player.position;
        if (playerPosition.x <= topRight.x && playerPosition.x >= bottomLeft.x && playerPosition.y <= topRight.y && playerPosition.y >= bottomLeft.y)
        {
            InventoryManager.currentBag = this;
            MakeInv();
        } else if (InventoryManager.currentBag == this)
        {
            InventoryManager.currentBag = null;
            HideInv();
        }
    }

    public void AddToBag(ItemUI item)
    {
        if (item == null)
        {
            if (bagItems.Count == 0)
            {
                HideInv();
                Destroy(this.gameObject);
            } 
            return;
        }
        if (item.myItem.itemTitle == "Empty")
        {
            if (bagItems.Count == 0)
            {
                HideInv();
                Destroy(item.gameObject);
                Destroy(this.gameObject);
            } 
            return;
        }
        bagItems.Add(item);
        if (bagItems.Count > allItems.Count)
        {
            //full bag, resize to make more room
            var layout = InventoryManager.bagInv.GetComponent<GridLayoutGroup>();
            for(int i = 0; i < layout.constraintCount; i++)
            {
                SlotUI slot = Instantiate(slotPrefab, InventoryManager.bagInv);
                slot.Initialize(emptyItem.itemType, -1);

                ItemUI itm = Instantiate(itemPrefab, slot.transform);
                itm.Initialize(emptyItem, slot, canvas);
                itm.GetComponent<Transform>().position = slot.GetComponent<Transform>().position;

                slot.UpdateItem(itm);
                allItems.Add(itm);
            }
            
            float spacing = layout.spacing.y;
            float cellSize = layout.cellSize.y;

            InventoryManager.bagInv.GetComponent<RectTransform>().sizeDelta += new Vector2(0, spacing+cellSize);
        }

        for(int i = 0; i < allItems.Count; i++)
        {
            var itm = allItems[i];
            if (itm == null || itm.myItem.itemTitle == "Empty")
            {
                item.UpdateSlot(itm.slotOrigin);
                itm.slotOrigin.UpdateItem(item);
                item.GetComponent<Transform>().position = itm.slotOrigin.GetComponent<Transform>().position;
                item.transform.SetParent(itm.slotOrigin.transform, true);

                allItems[i] = item;

                Destroy(itm.gameObject);
                break;
            }
        }
    }

    public void RemoveFromBag(ItemUI item)
    {
        if (item == null)
        {
            return;
        }
        if (bagItems.Contains(item) && item.myItem.itemTitle != "Empty")
        {
           bagItems.Remove(item);
            if (bagItems.Count == 0)
            {
                HideInv();
                Destroy(this.gameObject);
            } 
        }
    }

    public void MakeInv()
    {
        HideInv(); //just in case its already occupied
        InventoryManager.bagInv.gameObject.SetActive(true);

        var layout = InventoryManager.bagInv.GetComponent<GridLayoutGroup>();

        if (allItems == null || allItems.Count == 0)
        {
            for(int i = 0; i < layout.constraintCount; i++)
            {
                SlotUI slot = Instantiate(slotPrefab, InventoryManager.bagInv);
                slot.Initialize(emptyItem.itemType, -1);

                ItemUI item = Instantiate(itemPrefab, slot.transform);
                item.Initialize(emptyItem, slot, canvas);
                item.GetComponent<Transform>().position = slot.GetComponent<Transform>().position;

                slot.UpdateItem(item);
            }
            InventoryManager.bagInv.GetComponent<RectTransform>().sizeDelta = baseSize;
        } else
        {
            for(int i = 0; i < allItems.Count; i++)
            {
                SlotUI slot = Instantiate(slotPrefab, InventoryManager.bagInv);
                slot.Initialize(emptyItem.itemType, -1);

                ItemUI item = Instantiate(itemPrefab, slot.transform);
                item.Initialize(allItems[i].myItem, slot, canvas);
                item.GetComponent<Transform>().position = slot.GetComponent<Transform>().position;

                slot.UpdateItem(item);
            }
            int count = allItems.Count / layout.constraintCount;
            float spacing = layout.spacing.y;
            float cellSize = layout.cellSize.y;
            float paddingTop = layout.padding.top;
            float paddingBottom = layout.padding.bottom;

            InventoryManager.bagInv.GetComponent<RectTransform>().sizeDelta = new Vector2(baseSize.x, paddingTop + ((spacing+cellSize)*count) - spacing + paddingBottom);
        }
    }

    public void HideInv()
    {
        InventoryManager.bagInv.gameObject.SetActive(false);
        foreach (Transform  child in InventoryManager.bagInv)
        {
            Destroy(child.gameObject);
        }
        InventoryManager.bagInv.GetComponent<RectTransform>().sizeDelta = baseSize;
    }

    public void ReplaceFromSlot(ItemUI newItm, SlotUI targetSlot)
    {
        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i].slotOrigin == targetSlot)
            {
                allItems[i] = newItm;
                return;
            }
        }
    }

}
