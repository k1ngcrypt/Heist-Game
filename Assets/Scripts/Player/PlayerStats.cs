using UnityEngine;
using HeistGame.Objectives;

public class PlayerStats : MonoBehaviour {
    private float health = 100;
    private int maxHealth = 100;
    private int baseArmour = 0;
    public int additionalArmour = 0;
    private bool healthWarningTriggered = false;
    const float loweHealthThreshold = 0.2f;

    public void increaseHealth(int amount) {
        maxHealth += amount;
        health += amount;
        if (health > maxHealth * loweHealthThreshold) healthWarningTriggered = false;
    }
    public void heal(float amount) { 
        health = Mathf.Min(health + amount, maxHealth);
        if (health > maxHealth * loweHealthThreshold) healthWarningTriggered = false;
        HealthBarUI.Instance.UpdateHealthUI();
    }
    public void fullHeal() { health = maxHealth; healthWarningTriggered = false; HealthBarUI.Instance.UpdateHealthUI(); }
    public float getCurrentHealth() { return health; }
    public int getMaxHealth() { return maxHealth; }

    public void increaseBaseArmour(int amount) { baseArmour += amount; }
    public int getTotalArmour() { return baseArmour + additionalArmour; }

    private float armourReductionPercentage() {
        int effectiveArmour = baseArmour + additionalArmour;
        return 1 - Mathf.Exp(-(effectiveArmour*effectiveArmour/2809f)) * 0.95f;
    }

    public void takeDamage(float damage) {
        float damageAfterArmour = Mathf.Round((damage - (damage * armourReductionPercentage()))*100f) / 100.0f;
        health = Mathf.Max(0, health - damageAfterArmour);
        if (!healthWarningTriggered && health <= maxHealth * loweHealthThreshold) {
            healthWarningTriggered = true;
            NotificationManager.Instance.SendNotification("Health is low!", Color.red);
        }
        //Debug.Log($"Player took {damageAfterArmour} damage after armour reduction. Current health: {health}. Current Armour: {effectiveArmour}");
        HealthBarUI.Instance.UpdateHealthUI();
        if (health == 0) ObjectiveManager.Instance.FailObjective(0);
    }

}


