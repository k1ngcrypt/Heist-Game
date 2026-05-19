using UnityEngine;
using TMPro;

public class ItemOverlay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

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
