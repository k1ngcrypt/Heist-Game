using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TechNodeUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI statusText;
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
            titleText.text = string.IsNullOrWhiteSpace(tech.techName) ? tech.techID : tech.techName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = tech.description;
        }

        if (costText != null)
        {
            costText.text = $"Cost: {tech.resourceCost}";
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
            if (statusText != null) statusText.text = "Unlocked";
        }
        else if (techManager.CanUnlock(myTech))
        {
            if (iconImage != null) iconImage.color = new Color(0.8f, 0.8f, 0.8f);
            if (unlockButton != null) unlockButton.interactable = true;
            if (statusText != null) statusText.text = "Available";
        }
        else
        {
            if (iconImage != null) iconImage.color = new Color(0.2f, 0.2f, 0.2f);
            if (unlockButton != null) unlockButton.interactable = false;
            if (statusText != null) statusText.text = techManager.GetLockReason(myTech);
        }
    }

    private void AttemptUnlock()
    {
        techManager.UnlockTech(myTech);
    }
}