using UnityEngine;

public static class DamageUtils
{
    public static float ArmourReductionPercentage(int armour)
    {
        return 1 - Mathf.Exp(-(armour * armour / 2809f)) * 0.95f;
    }
}
