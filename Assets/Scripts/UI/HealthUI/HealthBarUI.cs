using TMPro;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private RectTransform fullHealthRT;
    [SerializeField] private RectTransform currentHealthRT;
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private PlayerStats playerStats;

    public static HealthBarUI Instance { get; private set;}

    public void OnValidate() {
        if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
    }
    

    public void Start()
    {
        Instance = this;
        UpdateHealthUI();
    }

    public void OnDestroy()
    {
        Instance = null;
    }

    public void UpdateHealthUI()
    {
        if (SettingManager.godMode)
        {
            percentText.text = $"Infinite";
            currentHealthRT.sizeDelta = new Vector2(fullHealthRT.sizeDelta.x, currentHealthRT.sizeDelta.y); 
        } else
        {
            float percent = Mathf.Clamp(playerStats.GetCurrentHealth() / playerStats.GetMaxHealth(),0f,1f);

            percentText.text = $"{Mathf.RoundToInt(100*percent)}%";
            currentHealthRT.sizeDelta = new Vector2(fullHealthRT.sizeDelta.x * percent, currentHealthRT.sizeDelta.y); 
        }
        
    }
}
