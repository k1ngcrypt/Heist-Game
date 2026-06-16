using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemOverlay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    public void Initialize(LoadoutItems item)
    {
        var nameRt = itemNameText.GetComponent<RectTransform>();
        if (itemNameText != null)
        {
            itemNameText.text = item.itemTitle;
            nameRt.sizeDelta = new Vector2(nameRt.sizeDelta.x, itemNameText.preferredHeight);
        }
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.itemDescription;
        }

        var boxRt = GetComponent<RectTransform>();
        
        float prefHeight = itemDescriptionText.preferredHeight;
        
        var layout = GetComponent<VerticalLayoutGroup>();
        float spacing = layout.spacing;
        float topPad = layout.padding.top;
        float bottomPad = layout.padding.bottom;

        if (prefHeight > 0)
        {
            boxRt.sizeDelta = new Vector2(boxRt.sizeDelta.x, nameRt.sizeDelta.y + prefHeight + spacing + topPad + bottomPad);
        } else
        {
            boxRt.sizeDelta = new Vector2(boxRt.sizeDelta.x, nameRt.sizeDelta.y + topPad + bottomPad);
        }
    }
}
