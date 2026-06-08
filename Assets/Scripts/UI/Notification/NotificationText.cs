using UnityEngine;
using TMPro;

public class NotificationText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI msgText;
    [SerializeField] private RectTransform pnlRct;

    private float yBuffer = 4;

    public void Initialize(string message, float time, Color color)
    {
        msgText.text = message;
        msgText.color = color;
        msgText.GetComponent<RectTransform>().sizeDelta = new Vector2(msgText.GetComponent<RectTransform>().sizeDelta.x, msgText.preferredHeight);
        pnlRct.sizeDelta = new Vector2(pnlRct.sizeDelta.x, msgText.preferredHeight + yBuffer);
        Destroy(gameObject, time);
    }
}
