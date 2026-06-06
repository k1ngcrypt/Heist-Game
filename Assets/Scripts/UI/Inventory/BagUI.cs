using System.Collections.Generic;
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
    public List<SlotUI> allSlots;

    private ItemUI itemPrefab;
    private SlotUI slotPrefab;
    private LoadoutItems emptyItem;

    private int rowSize;

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
        allSlots = new List<SlotUI>();

        this.itemPrefab = itemPrefab;
        this.slotPrefab = slotPrefab;
        this.emptyItem = emptyItem;

        rowSize = InventoryManager.bagInv.GetComponent<GridLayoutGroup>().constraintCount;

        InventoryManager.bagInv.gameObject.SetActive(true);
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

    public void DroppedItem(ItemUI item)
    {
        if (item == null)
        {
            if (bagItems.Count == 0)
            {
                DestroyBag();
            } 
            return;
        }
        if (item.myItem.itemTitle == "Empty")
        {
            if (bagItems.Count == 0)
            {
                Destroy(item.gameObject);
                DestroyBag();
            } 
            return;
        }
        bagItems.Add(item);
        if (bagItems.Count > allItems.Count)
        {
            //full bag or new bag, resize to make more room & create rows
            var layout = InventoryManager.bagInv.GetComponent<GridLayoutGroup>();
            for(int i = 0; i < rowSize; i++)
            {
                SlotUI slot = Instantiate(slotPrefab, InventoryManager.bagInv);
                slot.Initialize(emptyItem.itemType, -1);

                ItemUI itm = Instantiate(itemPrefab, slot.transform);
                itm.Initialize(emptyItem, slot, canvas);
                itm.GetComponent<Transform>().position = slot.GetComponent<Transform>().position;

                slot.UpdateItem(itm);

                allSlots.Add(slot);
                allItems.Add(itm);
            }
            
            float spacing = layout.spacing.y;
            float cellSize = layout.cellSize.y;

            if (allItems.Count == rowSize)
            {
                //new bag, make base size
                InventoryManager.bagInv.GetComponent<RectTransform>().sizeDelta = baseSize;
            } else
            {
                //made new row, increase size
                InventoryManager.bagInv.GetComponent<RectTransform>().sizeDelta += new Vector2(0, spacing+cellSize);
            }
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

    public void MakeInv()
    {
        HideInv(); //just in case its already occupied
        InventoryManager.bagInv.gameObject.SetActive(true);

        var layout = InventoryManager.bagInv.GetComponent<GridLayoutGroup>();

        if (allItems == null || allItems.Count == 0)
        {
            for(int i = 0; i < rowSize; i++)
            {
                SlotUI slot = Instantiate(slotPrefab, InventoryManager.bagInv);
                slot.Initialize(emptyItem.itemType, -1);

                ItemUI item = Instantiate(itemPrefab, slot.transform);
                item.Initialize(emptyItem, slot, canvas);
                item.GetComponent<Transform>().position = slot.GetComponent<Transform>().position;

                slot.UpdateItem(item);

                allSlots.Add(slot);
            }
            InventoryManager.bagInv.GetComponent<RectTransform>().sizeDelta = baseSize;
        } else
        {
            bagItems = new List<ItemUI>();
            for(int i = 0; i < allItems.Count; i++)
            {
                SlotUI slot = Instantiate(slotPrefab, InventoryManager.bagInv);
                slot.Initialize(emptyItem.itemType, -1);

                ItemUI item = Instantiate(itemPrefab, slot.transform);
                item.Initialize(allItems[i].myItem, slot, canvas);
                item.GetComponent<Transform>().position = slot.GetComponent<Transform>().position;

                slot.UpdateItem(item);
                
                allSlots.Add(slot);
                
                //fix null by redefining them
                allItems[i] = item;
                if (item.myItem.itemTitle != "Empty")
                {
                    bagItems.Add(item);
                }
            }
            int count = allItems.Count / rowSize;
            float spacing = layout.spacing.y;
            float cellSize = layout.cellSize.y;
            float paddingTop = layout.padding.top;
            float paddingBottom = layout.padding.bottom;

            InventoryManager.bagInv.GetComponent<RectTransform>().sizeDelta = new Vector2(baseSize.x, paddingTop + ((spacing+cellSize)*count) - spacing + paddingBottom);
        }
    }

    public void HideInv()
    {
        InventoryManager.Instance.CheckHeldItem();
        InventoryManager.bagInv.gameObject.SetActive(false);
        foreach (Transform  child in InventoryManager.bagInv)
        {
            Destroy(child.gameObject);
        }
        InventoryManager.bagInv.GetComponent<RectTransform>().sizeDelta = baseSize;
        allSlots = new List<SlotUI>();
    }

    public void ReplaceFromSlot(ItemUI newItm, SlotUI targetSlot)
    {
        if (newItm == null)
        {
            return;
        }
        for (int i = 0; i < allSlots.Count; i++)
        {
            if (allSlots[i] == targetSlot)
            {
                ItemUI oldItm = allItems[i];
                allItems[i] = newItm;
                if(oldItm.myItem.itemTitle != "Empty")
                {
                    //remove item from bag
                    bagItems.Remove(oldItm);
                    if (newItm.myItem.itemTitle == "Empty")
                    {
                        //size check
                        if(bagItems.Count == 0)
                        {
                            DestroyBag();
                            return;
                        }
                        if (allItems.Count - rowSize >= bagItems.Count)
                        {
                            //delete a row
                            var layout = InventoryManager.bagInv.GetComponent<GridLayoutGroup>();
                            int deleted = 0;
                            for(int j = 0; j < allItems.Count + deleted; j++)
                            {
                                if (deleted == rowSize)
                                {
                                    break;
                                }
                                if (allItems[j-deleted].myItem.itemTitle == "Empty")
                                {
                                    Destroy(allSlots[j-deleted].gameObject);
                                    Destroy(allItems[j-deleted].gameObject);

                                    allItems.Remove(allItems[j-deleted]);
                                    allSlots.Remove(allSlots[j-deleted]);
                                    deleted++;
                                }
                            }
                            
                            float spacing = layout.spacing.y;
                            float cellSize = layout.cellSize.y;

                            InventoryManager.bagInv.GetComponent<RectTransform>().sizeDelta -= new Vector2(0, spacing+cellSize);

                        }
                    }
                    
                }
                if (newItm.myItem.itemTitle != "Empty")
                {
                    bagItems.Add(newItm);  
                }
                return;
            }
        }
    }

    public void SwapItemsInList(ItemUI newItm, ItemUI oldItm)
    {
        int idx1 = -1;
        int idx2 = -1;
        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i] == newItm)
            {
                idx1 = i;
            } 
            if (allItems[i] == oldItm)
            {
                idx2 = i;
            }
        }
        if (idx1 == -1)
        {
            Debug.Log("No item found for old item");
            return;
        }
        if (idx2 == -1)
        {
            Debug.Log("No item found for old item");
            return;
        }
        ItemUI temp = allItems[idx1];
        allItems[idx1] = allItems[idx2];
        allItems[idx2] = temp;
    }

    public void DestroyBag()
    {
        HideInv();
        Destroy(this.gameObject);
        turnManager.Unregister(this);
        bagItems.Clear();
        allItems.Clear();
        allSlots.Clear();
        InventoryManager.currentBag = null;
    }

    public void DebugBag()
    {
        string str = "Bag Inv: ";
        for (int i = 0; i < allItems.Count; i++)
        {
            if(allItems[i] == null)
            {
                str += "null, ";
            } else
            {
               str += allItems[i].myItem.itemTitle + ", "; 
            }
            
        }
        Debug.Log(str);

        str = "Bag Items:";
        for (int i = 0; i < bagItems.Count; i++)
        {
            if(bagItems[i] == null)
            {
                str += "null, ";
            } else
            {
               str += bagItems[i].myItem.itemTitle + ", "; 
            }
        }
        Debug.Log(str);
    }

}
