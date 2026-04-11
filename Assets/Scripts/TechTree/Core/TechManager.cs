using UnityEngine;
using System;
using System.Collections.Generic;

public class TechManager : MonoBehaviour
{
    [Header("Tech Tree Data")]
    public List<TechNodeSO> allTechNodes = new();//full list of all techs
    public List<TechNodeSO> startingUnlockedTechs = new();

    [Header("Progression")]
    [Min(0)] public int availableTechPoints;

    private HashSet<string> unlockedTechIDs = new();//IDs of all unlocked techs

    public static event Action<TechNodeSO> OnTechUnlocked;//event for tech unlocks
    public static event Action OnTechStateChanged;

    private void Awake()
    {
        InitializeStartingTechs();
    }

    private void InitializeStartingTechs()
    {
        unlockedTechIDs.Clear();

        foreach (var tech in startingUnlockedTechs)
        {
            if (tech == null || string.IsNullOrWhiteSpace(tech.techID))
            {
                continue;
            }

            unlockedTechIDs.Add(tech.techID);
        }

        OnTechStateChanged?.Invoke();
    }

    public void UnlockTech(TechNodeSO tech)//function to unlock a tech
    {
        if (CanUnlock(tech))
        {
            unlockedTechIDs.Add(tech.techID);
            availableTechPoints -= tech.resourceCost;
            OnTechUnlocked?.Invoke(tech);
            OnTechStateChanged?.Invoke();
        }
    }

    public bool CanUnlock(TechNodeSO tech)
    {
        if (tech == null || string.IsNullOrWhiteSpace(tech.techID)) return false;
        if (unlockedTechIDs.Contains(tech.techID)) return false; //already unlocked
        if (tech.resourceCost > availableTechPoints) return false;

        //check prereqs (AND)
        if (tech.prerequisites != null)
        {
            foreach (var prereq in tech.prerequisites)
            {
                if (prereq == null || string.IsNullOrWhiteSpace(prereq.techID))
                    return false;

                if (!unlockedTechIDs.Contains(prereq.techID))
                    return false;
            }
        }

        return true;
    }

    public bool IsUnlocked(TechNodeSO tech)
    {
        return tech != null && !string.IsNullOrWhiteSpace(tech.techID) && IsUnlocked(tech.techID);
    }

    public bool IsUnlocked(string techID)
    {
        return unlockedTechIDs.Contains(techID);
    }

    public void AddTechPoints(int amount)
    {
        if (amount <= 0) return;

        availableTechPoints += amount;
        OnTechStateChanged?.Invoke();
    }
}