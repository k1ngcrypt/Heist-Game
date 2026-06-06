using UnityEngine;
using TMPro;

public class NotificationText : MonoBehaviour
{
    [SerializeField] private TMP_Text msgText;

    public void Initialize(string message, float time)
    {
        msgText.text = message;
        msgText.GetComponent<RectTransform>().sizeDelta = new Vector2(msgText.GetComponent<RectTransform>().sizeDelta.x, msgText.preferredHeight);
        Destroy(gameObject, time);
    }
}
