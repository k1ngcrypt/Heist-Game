using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class LoadoutBtnUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public LoadoutItems myItem;
    LoadoutManager loadoutManager;
    int idx;

    public void Initialize(int idx, LoadoutItems item, LoadoutManager loadoutMgr)
    {
        this.idx = idx;
        myItem = item;
        loadoutManager = loadoutMgr;

        if (iconImage != null)
        {
            iconImage.sprite = myItem.itemIcon;
        }
        Color color = iconImage.color;
        color.a = 1f;
        iconImage.color = color;

        color = GetComponent<Image>().color;
        color.a = 1f;
        GetComponent<Image>().color = color;

        GetComponent<Button>().onClick.AddListener(OnPressed);
    }

    public void makeInvisible()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
        }
        Color color = iconImage.color;
        color.a = 0f;
        iconImage.color = color;

        color = GetComponent<Image>().color;
        color.a = 0f;
        GetComponent<Image>().color = color;
    }

    public void UpdateItem(LoadoutItems item)
    {
        myItem = item;  

        if (iconImage != null)
        {
            iconImage.sprite = myItem.itemIcon;
        }
        Color color = iconImage.color;
        color.a = 1f;
        iconImage.color = color;

        color = GetComponent<Image>().color;
        color.a = 1f;
        GetComponent<Image>().color = color;
        GetComponent<Button>().enabled = true;
    }

    public void OnPressed()
    {
        loadoutManager.ItemPressed(idx, myItem);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        loadoutManager.CreateOverlay(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        loadoutManager.DestroyOverlay();
    }
}
