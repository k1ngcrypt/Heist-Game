using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Mono.Cecil.Cil;
using UnityEngine.UI;
using Unity.VisualScripting;

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
    public Transform itemScroll;
    public Transform contents;
    public TextMeshProUGUI itemTitleText1;
    public TextMeshProUGUI itemTitleText2;
    public TextMeshProUGUI itemTitleText3;
    public TextMeshProUGUI itemTitleText4;
    public Button leftBtn;
    public Button rightBtn;
    public Transform contentsA1;
    public Transform contentsA2;
    public Transform contentsA3;
    public Transform contentsA4;

    private List<LoadoutBtnUI> PlayerLoadout = new();
    float width;
    float height;
    float spacing;
    int leftMostIndex;
    int firstGadgetIndex;

    void Start()
    {
        width = itemPrefab.GetComponent<RectTransform>().rect.width;
        height = itemPrefab.GetComponent<RectTransform>().rect.height;
        spacing = contents.GetComponent<HorizontalLayoutGroup>().spacing;
        leftMostIndex = 0;

        GenerateItemList();
    }

    void GenerateItemList()
    {
        if (itemPrefab == null || itemScroll == null || contents == null || itemTitleText1 == null || itemTitleText2 == null || itemTitleText3 == null || itemTitleText4 == null || leftBtn == null || rightBtn == null)
        {
            Debug.LogWarning("LoadoutManager is missing references (ItemPrefab, ItemScroll, Contents, ItemTitleTexts, LeftBtn, RightBtn, ContentAreas).", this);
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
        TextMeshProUGUI text = null;
        Transform cont = null;
        if (leftMostIndex == idx) {
            text = itemTitleText1;
            cont = contentsA1;
        } else if (leftMostIndex + 1 == idx) {
            text = itemTitleText2;
            cont = contentsA2;
        } else if (leftMostIndex + 2 == idx) {
            text = itemTitleText3;
            cont = contentsA3;
        } else if (leftMostIndex + 3 == idx) {
            text = itemTitleText4;
            cont = contentsA4;
        } else {
            Debug.LogWarning("Invalid item index pressed: " + idx);
            return;
        }

        if (!cont.gameObject.activeSelf && text.gameObject.activeSelf)
        {
            //make that invisible
            text.gameObject.SetActive(false);
            cont.gameObject.SetActive(true);
            ShowItemList(idx, item, cont);
        } else if (cont.gameObject.activeSelf && !text.gameObject.activeSelf)
        {
            text.gameObject.SetActive(true);
            cont.gameObject.SetActive(false);
            SelectedItem(idx, item, cont);
        }
    }

    private void ShowItemList(int idx, LoadoutItems item, Transform cont)
    {
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

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            cont.GetComponent<RectTransform>()
        );
        var lastBtn = tempBtns[tempBtns.Count - 1];
        float pos = lastBtn.GetComponent<RectTransform>().anchoredPosition.y;

        cont.GetComponent<RectTransform>().sizeDelta = new Vector2(cont.GetComponent<RectTransform>().sizeDelta.x, -(pos - (height/2f)));

        for (int i = 0; i < tempBtns.Count; i++)
        {
            if (tempBtns[i].myItem != item)
            {
                cont.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, spacing + height);
            } else {
                tempBtns[i].GetComponent<Image>().color = Color.green;
                break;
            }
        }
        leftBtn.interactable = false;
        rightBtn.interactable = false;
    }

    private void SelectedItem(int idx, LoadoutItems item, Transform cont)
    {
        bool meet = false;
        foreach (Transform child in cont)
        {
            if (!meet && child.GetComponent<LoadoutBtnUI>().myItem != PlayerLoadout[idx].myItem)
            {
                cont.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, spacing + height);
            } else if (!meet) {
                meet = true;
            }
            Destroy(child.gameObject);
        }
        cont.GetComponent<RectTransform>().sizeDelta = new Vector2(cont.GetComponent<RectTransform>().sizeDelta.x, height); 
        
        PlayerLoadout[idx].Initialize(idx, item, this);
        PlayerLoadout[idx].name = item.name;
        checkBtns();
    }
}
