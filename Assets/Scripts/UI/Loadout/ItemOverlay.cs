using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class ItemOverlay : MonoBehaviour
{
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;

    public void Initialize(LoadoutItems item)
    {
        if (itemNameText != null)
        {
            itemNameText.text = item.itemTitle;
        }
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.itemDescription;
        }
    }
}
