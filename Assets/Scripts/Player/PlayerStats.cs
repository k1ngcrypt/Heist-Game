using UnityEngine;

public class PlayerStats : MonoBehaviour {
    private float health = 100;
    private int maxHealth = 100;
    private int baseArmour = 0;
    private bool healthWarningTriggered = false;
    const float lowHealthThreshold = 0.2f;

    public void IncreaseHealth(int amount) {
        maxHealth += amount;
        health += amount;
        if (health > maxHealth * lowHealthThreshold) healthWarningTriggered = false;
    }
    public void Heal(float amount) { 
        health = Mathf.Min(health + amount, maxHealth);
        if (health > maxHealth * lowHealthThreshold) healthWarningTriggered = false;
        HealthBarUI.Instance.UpdateHealthUI();
    }
    public void FullHeal() { health = maxHealth; healthWarningTriggered = false; HealthBarUI.Instance.UpdateHealthUI(); }
    public float GetCurrentHealth() { return health; }
    public int GetMaxHealth() { return maxHealth; }

    public void IncreaseBaseArmour(int amount) { baseArmour += amount; }
    public int GetTotalArmour() {
        LoadoutItems additionalArmour = InventoryManager.Instance.ReturnEquipArmour();
        return baseArmour + ((additionalArmour.itemTitle != "Empty")? ((ArmourItem)additionalArmour).armourValue : 0);
    }

    public void TakeDamage(float damage) {
        if (SettingManager.Instance.godMode) return;
        float damageAfterArmour = Mathf.Round((damage - (damage * DamageUtils.ArmourReductionPercentage(GetTotalArmour())))*100f) / 100.0f;
        health = Mathf.Max(0, health - damageAfterArmour);
        if (!healthWarningTriggered && health <= maxHealth * lowHealthThreshold) {
            healthWarningTriggered = true;
            NotificationManager.Instance.SendNotification("Health is low!", Color.red);
        }
        HealthBarUI.Instance.UpdateHealthUI();
        if (health == 0) SceneUIManager.Instance.MissionFailed("You Died");
    }

    public bool IsDisguised() {
        ArmourItem Armour = InventoryManager.Instance.ReturnEquipArmour() as ArmourItem;
        return Armour != null && Armour.isDisguise;
    }

}


