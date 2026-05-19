using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionOverlay : MonoBehaviour
{
    string txt;
    [SerializeField] private GameObject interactionOverlay;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button interactButton;

    public delegate void InteractAction();

    public void Start()
    {
        interactionOverlay.SetActive(false);
    }

    public void Initialize(string title, InteractAction action)
    {
        titleText.text = title;
        interactButton.onClick.RemoveAllListeners();
        interactButton.onClick.AddListener(() => action());
        interactionOverlay.SetActive(true);
    }
}
