using TMPro;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private RectTransform fullHealthRT;
    [SerializeField] private RectTransform currentHealthRT;
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private PlayerStats playerStats;

    public static HealthBarUI Instance { get; private set;}

    public void Start()
    {
        Instance = this;
        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        float percent = playerStats.getCurrentHealth() / playerStats.getMaxHealth();
        if (percent < 0) percent = 0;
        if (percent > 1) percent = 1;

        percentText.text = $"{(int)(100*percent)}%";
        currentHealthRT.sizeDelta = new Vector2(fullHealthRT.sizeDelta.x * percent, currentHealthRT.sizeDelta.y);
    }
}
