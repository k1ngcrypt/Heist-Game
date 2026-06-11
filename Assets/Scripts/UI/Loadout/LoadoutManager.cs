using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class AreaList
{
    [SerializeField] public Transform cont;
    [SerializeField] public Transform area;
    [SerializeField] public Scrollbar scrollBar;
}

public class LoadoutManager : MonoBehaviour
{
    [Header("Items")]
    [SerializeField] private List<LoadoutItems> allArmours = new();
    [SerializeField] private List<LoadoutItems> allWeapons = new();
    [SerializeField] private List<LoadoutItems> allGadgets = new();
    [SerializeField] private LoadoutItems emptyArmour;
    [SerializeField] private LoadoutItems emptyWeapon;
    [SerializeField] private LoadoutItems emptyGadget;
    [SerializeField] public int armourCount = 1;

    [SerializeField] public int weaponsCount = 1;
    [SerializeField] public int startingGadgetsCount = 3;


    [Header("References")]
    [SerializeField] private LoadoutBtnUI itemPrefab;
    [SerializeField] private ItemOverlay itemOverlayPrefab;
    [SerializeField] private Transform itemList;    
    [SerializeField] private Transform itemScroll;
    [SerializeField] private Transform contents;
    [SerializeField] private TextMeshProUGUI itemTitleText1;
    [SerializeField] private TextMeshProUGUI itemTitleText2;
    [SerializeField] private TextMeshProUGUI itemTitleText3;
    [SerializeField] private TextMeshProUGUI itemTitleText4;
    [SerializeField] private Button leftBtn;
    [SerializeField] private Button rightBtn;
    [SerializeField] private List<AreaList> ContentAreas = new(4); 

    private readonly List<LoadoutBtnUI> PlayerLoadout = new();
    private float width;
    private float height;
    private float spacing;
    private int leftMostIndex;
    private int firstGadgetIndex;
    private ItemOverlay currentOverlay;
    private List<TextMeshProUGUI> returnText = new();
    private Color selectedColour = new Color(215f/255f, 255f/255f, 187f/255f, 207f/255f); //color can't be declared const

    //Get Component Variables
    private RectTransform contentsRT;

    void Start()
    {
        width = itemPrefab.GetComponent<RectTransform>().rect.width;
        height = itemPrefab.GetComponent<RectTransform>().rect.height;
        spacing = contents.GetComponent<HorizontalLayoutGroup>().spacing;
        leftMostIndex = 0;
        currentOverlay = null;
        returnText = new List<TextMeshProUGUI> { itemTitleText1, itemTitleText2, itemTitleText3, itemTitleText4 };
        contentsRT = contents.GetComponent<RectTransform>();
        InventoryManager.Instance.startingArmour = armourCount;
        InventoryManager.Instance.startingWeapons = weaponsCount;

        foreach (AreaList box in ContentAreas)
        {
            box.area.gameObject.SetActive(false);
        }

        GenerateItemList();
    }

    void GenerateItemList()
    {
         if (!itemPrefab || !itemScroll || !contents || !itemTitleText1 || !itemTitleText2 || !itemTitleText3 || !itemTitleText4 || !leftBtn || !rightBtn)
        {
            Debug.LogWarning("LoadoutManager is missing references (ItemPrefab, ItemScroll, Contents, ItemTitleTexts, LeftBtn, RightBtn, ContentAreas, Areas).", this);
            return;
        }

        for (int i = 0; i < armourCount + weaponsCount + startingGadgetsCount; i++)
        {
            LoadoutItems item;
            if (i < armourCount) {
                item = emptyArmour;
            } else if (i < armourCount + weaponsCount) {
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
        CheckBtns();
    }

    private void SetText()
    {
        //code below sets the text for the 4 visible items
        // text = item type + number (if gadget)
        itemTitleText1.text = PlayerLoadout[leftMostIndex].myItem.GetType().Name + " " + ((leftMostIndex) - firstGadgetIndex >= 0 ? (leftMostIndex) - firstGadgetIndex + 1 : " ");
        itemTitleText2.text = PlayerLoadout[leftMostIndex + 1].myItem.GetType().Name + " " + ((leftMostIndex+1) - firstGadgetIndex >= 0 ? (leftMostIndex+1) - firstGadgetIndex + 1 : " ");
        itemTitleText3.text = PlayerLoadout[leftMostIndex + 2].myItem.GetType().Name + " " + ((leftMostIndex+2) - firstGadgetIndex >= 0 ? (leftMostIndex+2) - firstGadgetIndex + 1 : " ");
        itemTitleText4.text = PlayerLoadout[leftMostIndex + 3].myItem.GetType().Name + " " + ((leftMostIndex+3) - firstGadgetIndex >= 0 ? (leftMostIndex+3) - firstGadgetIndex + 1 : " ");
    }

    private void ResizeContent()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentsRT);
        var btn = PlayerLoadout[^1];
        float pos = btn.GetComponent<RectTransform>().anchoredPosition.x;

        contentsRT.sizeDelta = new Vector2(pos + (width*0.5f), contentsRT.sizeDelta.y);
    }

    public void ScrollLeft()
    {
        if (leftMostIndex == 0) return;
        contentsRT.anchoredPosition += new Vector2(width + spacing, 0);
        leftMostIndex--;
        SetText();
        CheckBtns();
    }

    public void ScrollRight()
    {
        if (leftMostIndex + 4 >= PlayerLoadout.Count) return;
        //move contents
        contentsRT.anchoredPosition -= new Vector2(width + spacing, 0);
        leftMostIndex++;
        SetText();
        CheckBtns();
    }

    private void CheckBtns()
    {
        if (leftMostIndex == 0) {
            leftBtn.interactable = false;
        } else {
            leftBtn.interactable = true;
        }

        if (leftMostIndex + 4 >= PlayerLoadout.Count) { //leftMostIndex + four is the right most visible item
            rightBtn.interactable = false;
        } else {
            rightBtn.interactable = true;
        }
    }

    public void ItemPressed(int idx, LoadoutItems item)
    {
        DestroyOverlay();
        TextMeshProUGUI text = returnText[idx - leftMostIndex];
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
            SelectedItem(idx, item, cont);
        }
    }

    private void ShowItemList(int idx, LoadoutItems item, Transform area, Transform cont, Scrollbar scrollbar)
    {
        var content = cont.GetComponent<RectTransform>();
        PlayerLoadout[idx].MakeInvisible();

        string type = item.GetType().Name;
        List<LoadoutItems> itemsToShow = new();
        if (type == "ArmourItem") {
            itemsToShow = allArmours;
        } else if (type == "WeaponItem") {
            itemsToShow = allWeapons;
        } else if (type == "GadgetItem") {
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
        layout.padding.top = Mathf.RoundToInt(padding*0.5f);
        layout.padding.bottom = Mathf.RoundToInt(padding*0.5f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        var lastBtn = tempBtns[^1];
        float pos = lastBtn.GetComponent<RectTransform>().anchoredPosition.y;

        content.sizeDelta = new Vector2(content.sizeDelta.x, -(pos - (height*0.5f) - padding*0.5f));

        for (int i = 0; i < tempBtns.Count; i++)
        {
            if (tempBtns[i].myItem == item)
            {
                area.GetComponent<ScrollRect>().verticalNormalizedPosition = 1f - (i / (float)(tempBtns.Count - 1));
                scrollbar.value = 1f - (i / (float)(tempBtns.Count - 1));
                tempBtns[i].GetComponent<Image>().color = selectedColour;
                break;
            }
        }
        scrollbar.numberOfSteps = tempBtns.Count;
        leftBtn.interactable = false;
        rightBtn.interactable = false;
    }

    private void SelectedItem(int idx, LoadoutItems item, Transform cont)
    {
        var content = cont.GetComponent<RectTransform>();

        foreach (Transform child in cont)
        {
            Destroy(child.gameObject);
        }
        content.sizeDelta = new Vector2(content.sizeDelta.x, height); 
        
        PlayerLoadout[idx].UpdateItem(item);
        PlayerLoadout[idx].name = item.name;
        CheckBtns();
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
        float offset = 10f;

        currentOverlay.GetComponent<RectTransform>().localPosition = localPos - new Vector3(width*0.5f + btnOrigin.GetComponent<RectTransform>().rect.width*0.5f + offset, 0, 0);
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
                returnText[i].gameObject.SetActive(true);
            }
        }
    }
}
