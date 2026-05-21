using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InteractionOverlay : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Transform panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button btnPrefab;

    public void Initialize(string title, List<InteractBtnTemplate> actions, Vector3 worldPosition)
    {
        titleText.text = title;
        canvas.GetComponent<Transform>().position = worldPosition - new Vector3(0, 1.25f, 0); // One and a quartertile down

        List<Button> buttons = new List<Button>();
        foreach (var btnData in actions)
        {
            Button btn = Instantiate(btnPrefab, panel);
            btn.GetComponentInChildren<TMP_Text>().text = btnData.text;

            btn.onClick.AddListener(() =>
            {
                btnData.onClick?.Invoke();
            });
            buttons.Add(btn);
        }

        //resize panel to fit buttons
        var layout = panel.GetComponent<VerticalLayoutGroup>();
        float paddingBottom = layout.padding.bottom;

        var pnl = panel.GetComponent<RectTransform>();

        LayoutRebuilder.ForceRebuildLayoutImmediate(pnl);
        var lastBtn = buttons[^1];
        float pos = lastBtn.GetComponent<RectTransform>().anchoredPosition.y;
        float height = lastBtn.GetComponent<RectTransform>().rect.height;


        pnl.sizeDelta = new Vector2(pnl.sizeDelta.x, -(pos - (height*0.5f) - paddingBottom));
    }
}
