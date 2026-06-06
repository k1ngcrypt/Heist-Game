using UnityEngine;
using TMPro;

public class NotificationText : MonoBehaviour
{
    [SerializeField] private TMP_Text msgText;
    [SerializeField] private RectTransform txtRct;

    public void Initialize(string message, float time, Color color)
    {
        msgText.text = message;
        msgText.color = color;
        txtRct.sizeDelta = new Vector2(txtRct.sizeDelta.x, msgText.preferredHeight);
        Destroy(gameObject, time);
    }
}
