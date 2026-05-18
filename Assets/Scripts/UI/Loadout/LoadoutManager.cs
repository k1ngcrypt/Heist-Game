using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class AreaList
{
    public Transform cont;
    public Transform area;
    public Scrollbar scrollBar;
}

public class LoadoutManager : MonoBehaviour
{
    [Header("Items")]
    public List<LoadoutItems> allArmours = new();
    public List<LoadoutItems> allWeapons = new();
    public List<LoadoutItems> allGadgets = new();
    public LoadoutItems emptyArmour;
    public LoadoutItems emptyWeapon;
    public LoadoutItems emptyGadget;
    public int startingGadgetsCount = 3;


    [Header("References")]
    public LoadoutBtnUI itemPrefab;
    public ItemOverlay itemOverlayPrefab;
    public Transform itemList;    
    public Transform itemScroll;
    public Transform contents;
    public TextMeshProUGUI itemTitleText1;
    public TextMeshProUGUI itemTitleText2;
    public TextMeshProUGUI itemTitleText3;
    public TextMeshProUGUI itemTitleText4;
    public Button leftBtn;
    public Button rightBtn;
    [SerializeField] private List<AreaList> ContentAreas = new(4); 

    private List<LoadoutBtnUI> PlayerLoadout = new();
    float width;
    float height;
    float spacing;
    int leftMostIndex;
    int firstGadgetIndex;
    ItemOverlay currentOverlay;

    void Start()
    {
        width = itemPrefab.GetComponent<RectTransform>().rect.width;
        height = itemPrefab.GetComponent<RectTransform>().rect.height;
        spacing = contents.GetComponent<HorizontalLayoutGroup>().spacing;
        leftMostIndex = 0;
        currentOverlay = null;

        foreach (AreaList box in ContentAreas)
        {
            box.area.gameObject.SetActive(false);
        }

        GenerateItemList();
    }

    void GenerateItemList()
    {
        if (itemPrefab == null || itemScroll == null || contents == null || itemTitleText1 == null || itemTitleText2 == null || itemTitleText3 == null || itemTitleText4 == null || leftBtn == null || rightBtn == null)
        {
            Debug.LogWarning("LoadoutManager is missing references (ItemPrefab, ItemScroll, Contents, ItemTitleTexts, LeftBtn, RightBtn, ContentAreas, Areas).", this);
            return;
        }

        for (int i = 0; i < 2+startingGadgetsCount; i++)
        {
            LoadoutItems item;
            if (i == 0) {
                item = emptyArmour;
            } else if (i == 1) {
                item = emptyWeapon;
            } else {
                item = emptyGadget;
            }
            var btn = Instantiate(itemPrefab, contents);
            btn.name = item.name;
            btn.Initialize(i, item, this);
            btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            PlayerLoadout.Add(btn);
        }
        firstGadgetIndex = 2;

        SetText();
        ResizeContent();
        checkBtns();
    }

    private void SetText()
    {
        itemTitleText1.text = PlayerLoadout[leftMostIndex].myItem.itemType + " " + ((leftMostIndex) - firstGadgetIndex >= 0 ? (leftMostIndex) - firstGadgetIndex + 1 : " ");
        itemTitleText2.text = PlayerLoadout[leftMostIndex + 1].myItem.itemType + " " + ((leftMostIndex+1) - firstGadgetIndex >= 0 ? (leftMostIndex+1) - firstGadgetIndex + 1 : " ");
        itemTitleText3.text = PlayerLoadout[leftMostIndex + 2].myItem.itemType + " " + ((leftMostIndex+2) - firstGadgetIndex >= 0 ? (leftMostIndex+2) - firstGadgetIndex + 1 : " ");
        itemTitleText4.text = PlayerLoadout[leftMostIndex + 3].myItem.itemType + " " + ((leftMostIndex+3) - firstGadgetIndex >= 0 ? (leftMostIndex+3) - firstGadgetIndex + 1 : " ");
    }

    private void ResizeContent()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            contents.GetComponent<RectTransform>()
        );
        var btn = PlayerLoadout[PlayerLoadout.Count - 1];
        float pos = btn.GetComponent<RectTransform>().anchoredPosition.x;

        contents.GetComponent<RectTransform>().sizeDelta = new Vector2(pos + (width/2f), contents.GetComponent<RectTransform>().sizeDelta.y);
    }

    public void ScrollLeft()
    {
        if (leftMostIndex == 0) return;
        contents.GetComponent<RectTransform>().anchoredPosition += new Vector2(width + spacing, 0);
        leftMostIndex--;
        SetText();
        checkBtns();
    }

    public void ScrollRight()
    {
        if (leftMostIndex + 4 >= PlayerLoadout.Count) return;
        //move contents
        contents.GetComponent<RectTransform>().anchoredPosition -= new Vector2(width + spacing, 0);
        leftMostIndex++;
        SetText();
        checkBtns();
    }

    private void checkBtns()
    {
        if (leftMostIndex == 0) {
            leftBtn.interactable = false;
        } else {
            leftBtn.interactable = true;
        }

        if (leftMostIndex + 4 >= PlayerLoadout.Count) {
            rightBtn.interactable = false;
        } else {
            rightBtn.interactable = true;
        }
    }

    public void ItemPressed(int idx, LoadoutItems item)
    {
        DestroyOverlay();
        TextMeshProUGUI text = returnText(idx - leftMostIndex);
        Transform cont = ContentAreas[idx - leftMostIndex].cont;
        Transform area = ContentAreas[idx - leftMostIndex].area;
        Scrollbar scrollbar = ContentAreas[idx - leftMostIndex].scrollBar;

        if (!area.gameObject.activeSelf && text.gameObject.activeSelf)
        {
            text.gameObject.SetActive(false);
            area.gameObject.SetActive(true);
            ShowItemList(idx, item, area, cont, scrollbar);
        } else 
        {
            text.gameObject.SetActive(true);
            area.gameObject.SetActive(false);
            SelectedItem(idx, item, area, cont, scrollbar);
        }
    }

    private void ShowItemList(int idx, LoadoutItems item, Transform area, Transform cont, Scrollbar scrollbar)
    {
        var content = cont.GetComponent<RectTransform>();
        PlayerLoadout[idx].makeInvisible();

        ItemType type = item.itemType;
        List<LoadoutItems> itemsToShow = new();
        if (type == ItemType.Armour) {
            itemsToShow = allArmours;
        } else if (type == ItemType.Weapon) {
            itemsToShow = allWeapons;
        } else if (type == ItemType.Gadget) {
            itemsToShow = allGadgets;
        }
        List<LoadoutBtnUI> tempBtns = new();
        foreach (var it in itemsToShow)
        {
            var btn = Instantiate(itemPrefab, cont);
            btn.name = it.name;
            btn.Initialize(idx, it, this);
            btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            tempBtns.Add(btn);
        }
        float padding = area.GetComponent<RectTransform>().rect.height - height;

        var layout = cont.GetComponent<VerticalLayoutGroup>();
        layout.padding.top = Mathf.RoundToInt(padding/2f);
        layout.padding.bottom = Mathf.RoundToInt(padding/2f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        var lastBtn = tempBtns[tempBtns.Count - 1];
        float pos = lastBtn.GetComponent<RectTransform>().anchoredPosition.y;

        content.sizeDelta = new Vector2(content.sizeDelta.x, -(pos - (height/2f) - padding/2f));

        for (int i = 0; i < tempBtns.Count; i++)
        {
            if (tempBtns[i].myItem == item)
            {
                area.GetComponent<ScrollRect>().verticalNormalizedPosition = 1f - (i / (float)(tempBtns.Count - 1));
                scrollbar.value = 1f - (i / (float)(tempBtns.Count - 1));
                tempBtns[i].GetComponent<Image>().color = new Color(215f, 255f, 187f, 207f)/255f;
                break;
            }
        }
        scrollbar.numberOfSteps = tempBtns.Count;
        leftBtn.interactable = false;
        rightBtn.interactable = false;
    }

    private void SelectedItem(int idx, LoadoutItems item, Transform area, Transform cont, Scrollbar scrollbar)
    {
        var content = cont.GetComponent<RectTransform>();

        foreach (Transform child in cont)
        {
            Destroy(child.gameObject);
        }
        content.sizeDelta = new Vector2(content.sizeDelta.x, height); 
        
        PlayerLoadout[idx].UpdateItem(item);
        PlayerLoadout[idx].name = item.name;
        checkBtns();
    }

    private TextMeshProUGUI returnText(int num)
    {
        if (num == 0) {
            return itemTitleText1;
        } else if (num == 1) {
            return itemTitleText2;
        } else if (num == 2) {
            return itemTitleText3;
        } else if (num == 3) {
            return itemTitleText4;
        } else {
            Debug.LogWarning("Invalid text number: " + num);
            return null;
        }
    }

    public List<LoadoutItems> GetCurrentLoadout()
    {
        List<LoadoutItems> currentLoadout = new();
        foreach (var item in PlayerLoadout)
        {
            currentLoadout.Add(item.myItem);
        }
        return currentLoadout;
    }

    public void CreateOverlay(LoadoutBtnUI btnOrigin)
    {
        DestroyOverlay();
        currentOverlay = Instantiate(itemOverlayPrefab, itemList);
        currentOverlay.Initialize(btnOrigin.myItem);

        Vector3 worldPos = btnOrigin.GetComponent<RectTransform>().position;
        Vector3 localPos = itemList.InverseTransformPoint(worldPos);

        currentOverlay.GetComponent<RectTransform>().localPosition = localPos - new Vector3(width/2f + btnOrigin.GetComponent<RectTransform>().rect.width/2f + 10, 0, 0);
    }

    public void DestroyOverlay()
    {
        if (currentOverlay != null)
        {
            Destroy(currentOverlay.gameObject);
            currentOverlay = null;
        }
    }

    public void OnDisable()
    {
        for (int i = 0; i < ContentAreas.Count; i++)
        {
            AreaList box = ContentAreas[i];
            if (box.area.gameObject.activeSelf)
            {
                foreach (Transform child in box.cont)
                {
                    Destroy(child.gameObject);
                }
                box.cont.GetComponent<RectTransform>().sizeDelta = new Vector2(box.cont.GetComponent<RectTransform>().sizeDelta.x, height);
                box.area.gameObject.SetActive(false);
                PlayerLoadout[i+leftMostIndex].UpdateItem(PlayerLoadout[i+leftMostIndex].myItem);
                returnText(i).gameObject.SetActive(true);
            }
        }
    }
}
