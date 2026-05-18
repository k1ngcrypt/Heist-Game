using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class LoadoutBtnUI : MonoBehaviour
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
        GetComponent<Button>().onClick.AddListener(OnPressed);
    }

    public void OnPressed()
    {
        loadoutManager.ItemPressed(idx, myItem);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Do something here
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //Do something here
    }
}
