using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TechNodeUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI costText;
    public Image iconImage;
    public Button unlockButton;

    private TechNodeSO myTech;
    private TechManager techManager;

    public void Initialize(TechNodeSO tech, TechManager manager)
    {
        myTech = tech;
        techManager = manager;

        if (titleText != null)
        {
            titleText.text = tech.techName;
        }

        if (costText != null)
        {
            costText.text = tech.resourceCost.ToString();
        }

        if (iconImage != null)
        {
            iconImage.sprite = tech.techIcon;
        }

        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(AttemptUnlock);
        }

        UpdateVisualState();
    }

    public void UpdateVisualState()
    {
        if (myTech == null || techManager == null)
        {
            return;
        }

        if (techManager.IsUnlocked(myTech))
        {
            if (iconImage != null) iconImage.color = Color.white;
            if (unlockButton != null) unlockButton.interactable = false;
        }
        else if (techManager.CanUnlock(myTech))
        {
            if (iconImage != null) iconImage.color = new Color(0.8f, 0.8f, 0.8f);
            if (unlockButton != null) unlockButton.interactable = true;
        }
        else
        {
            if (iconImage != null) iconImage.color = new Color(0.2f, 0.2f, 0.2f);
            if (unlockButton != null) unlockButton.interactable = false;
        }
    }

    private void AttemptUnlock()
    {
        techManager.UnlockTech(myTech);
    }
}