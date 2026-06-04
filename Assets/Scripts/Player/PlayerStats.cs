using UnityEngine;
using HeistGame.Objectives;

public class PlayerStats : MonoBehaviour {
    private float health = 100;
    private int maxHealth = 100;
    private int baseArmour = 0;
    public int additionalArmour = 0;

    public void increaseHealth(int amount) {
        maxHealth += amount;
        health += amount;
    }
    public void heal(float amount) { health = Mathf.Min(health + amount, maxHealth); }
    public void fullHeal() { health = maxHealth; }

    public void increaseBaseArmour(int amount) { baseArmour += amount; }

    public void takeDamage(float damage) {
        int effectiveArmour = baseArmour + additionalArmour;
        float damageAfterArmour = Mathf.Round((damage - (damage * (1 - Mathf.Exp(-(effectiveArmour*effectiveArmour/2809f)) * 0.95f))) * 100f) / 100.0f;
        health = Mathf.Max(0, health - damageAfterArmour);
        //Debug.Log($"Player took {damageAfterArmour} damage after armour reduction. Current health: {health}. Current Armour: {effectiveArmour}");
        if (health == 0) ObjectiveManager.Instance.FailObjective(0);
    }

}


