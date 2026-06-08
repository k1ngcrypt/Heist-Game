using UnityEngine;
using TMPro;

public class ObjectiveItem : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI tittleText;
    [SerializeField] private RectTransform textRT;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Color failedColor = Color.red;

    public void Initialize(string title, int current, int required, bool isCompleted, bool isFailed) {
        tittleText.text = title;
        tittleText.color = activeColor;
        if (required>1) tittleText.text += $" ({current}/{required})";
        if (isCompleted) {
            tittleText.color = completedColor;
            tittleText.text += " (Completed)";
        }
        else if (isFailed) {
            tittleText.text += " (Failed)";
            tittleText.color = failedColor;
        }
        textRT.sizeDelta = new Vector2(textRT.sizeDelta.x, tittleText.preferredHeight);
    }
}
