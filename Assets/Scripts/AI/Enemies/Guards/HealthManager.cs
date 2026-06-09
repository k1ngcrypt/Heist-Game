using Guards;
using System;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    public int MaxHealth => maxHealth;
    [SerializeField] private int armourPoints;
    [SerializeField] GuardStateManager guardStateManager;
    [SerializeField] private GameObject bloodPuddlePrefab;

    public float Health { get; private set; }

    private void Awake()
    {
        Health = maxHealth;

    }


    public void TakeDamage(float damage)
    {
        float damageAfterArmour = Mathf.Round((damage - (damage * DamageUtils.ArmourReductionPercentage(armourPoints))) * 100f) / 100.0f;
        Health -= damageAfterArmour;
        if (Health <= 0)
        {
            Instantiate(bloodPuddlePrefab, transform.position, transform.rotation);
            guardStateManager.Die();
            return;
        }
        guardStateManager.ReportDamage();
    }

    public void Heal(float amount)
    {
        Health = Mathf.Min(Health + amount, MaxHealth);
    }
}
