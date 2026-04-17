using UnityEngine;
using System;
using System.Collections.Generic;

public class TechManager : MonoBehaviour
{
    [Serializable]
    private class TechProgressData
    {
        public int availableTechPoints;
        public List<string> unlockedTechIDs = new();
    }

    [Header("Tech Tree Data")]
    public TechTreeSO techTree;
    public List<TechNodeSO> allTechNodes = new();//full list of all techs
    public List<TechNodeSO> startingUnlockedTechs = new();

    [Header("Progression")]
    [Min(0)] public int availableTechPoints;
    [Header("Currency Integration")]
    public CurrencyManager currencyManager;
    public bool useKnowledgeCurrency = true;
    public bool loadProgressOnAwake;
    public bool saveProgressOnChange = true;
    public string saveSlot = "default";

    private HashSet<string> unlockedTechIDs = new();//IDs of all unlocked techs
    private string SaveKey => $"TechTree.Progress.{saveSlot}";

    public static event Action<TechNodeSO> OnTechUnlocked;//event for tech unlocks
    public static event Action OnTechStateChanged;

    private int CurrentTechPoints
    {
        get
        {
            if (useKnowledgeCurrency && currencyManager != null)
            {
                return currencyManager.Get(CurrencyType.Knowledge);
            }

            return availableTechPoints;
        }
        set
        {
            var clampedValue = Mathf.Max(0, value);

            if (useKnowledgeCurrency && currencyManager != null)
            {
                currencyManager.Set(CurrencyType.Knowledge, clampedValue);
            }

            availableTechPoints = clampedValue;
        }
    }

    private void Awake()
    {
        if (currencyManager == null)
        {
            currencyManager = CurrencyManager.Instance;
        }

        SyncFromTreeAsset();

        var loaded = loadProgressOnAwake && LoadProgress();
        if (!loaded)
        {
            InitializeStartingTechs();
        }

        OnTechStateChanged?.Invoke();
    }

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged += HandleCurrencyChanged;
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    private void HandleCurrencyChanged(CurrencyType currencyType, int value, int delta)
    {
        if (!useKnowledgeCurrency || currencyType != CurrencyType.Knowledge)
        {
            return;
        }

        availableTechPoints = value;
        OnTechStateChanged?.Invoke();
    }

    private void OnValidate()
    {
        SyncFromTreeAsset();
    }

    private void SyncFromTreeAsset()
    {
        if (techTree == null)
        {
            return;
        }

        if (techTree.allTechNodes != null && techTree.allTechNodes.Count > 0)
        {
            allTechNodes = techTree.allTechNodes;
        }

        if (techTree.startingUnlockedTechs != null && techTree.startingUnlockedTechs.Count > 0)
        {
            startingUnlockedTechs = techTree.startingUnlockedTechs;
        }
    }

    private void InitializeStartingTechs()
    {
        unlockedTechIDs.Clear();

        foreach (var tech in GetStartingUnlockedTechs())
        {
            if (tech == null || string.IsNullOrWhiteSpace(tech.techID))
            {
                continue;
            }

            unlockedTechIDs.Add(tech.techID);
        }
    }

    public void UnlockTech(TechNodeSO tech)//function to unlock a tech
    {
        if (CanUnlock(tech))
        {
            unlockedTechIDs.Add(tech.techID);
            CurrentTechPoints -= tech.resourceCost;
            OnTechUnlocked?.Invoke(tech);

            if (saveProgressOnChange)
            {
                SaveProgress();
            }

            OnTechStateChanged?.Invoke();
        }
    }

    public bool CanUnlock(TechNodeSO tech)
    {
        return string.IsNullOrEmpty(GetLockReason(tech));
    }

    public string GetLockReason(TechNodeSO tech)
    {
        if (tech == null) return "Missing tech reference.";
        if (string.IsNullOrWhiteSpace(tech.techID)) return "Tech has no techID.";
        if (unlockedTechIDs.Contains(tech.techID)) return "Already unlocked.";
        if (tech.resourceCost > CurrentTechPoints) return $"Requires {tech.resourceCost} knowledge.";

        if (!ArePrerequisitesSatisfied(tech, out var missingPrereqNames))
        {
            if (tech.prerequisiteMode == PrerequisiteMode.Any)
            {
                return $"Requires any of: {missingPrereqNames}.";
            }

            return $"Missing prerequisites: {missingPrereqNames}.";
        }

        return string.Empty;
    }

    private bool ArePrerequisitesSatisfied(TechNodeSO tech, out string missingPrereqNames)
    {
        missingPrereqNames = string.Empty;
        if (tech.prerequisites == null || tech.prerequisites.Count == 0)
        {
            return true;
        }

        var missing = new List<string>();
        var hasAnyUnlocked = false;

        foreach (var prereq in tech.prerequisites)
        {
            if (prereq == null || string.IsNullOrWhiteSpace(prereq.techID))
            {
                missing.Add("[Invalid Prerequisite]");
                continue;
            }

            var unlocked = unlockedTechIDs.Contains(prereq.techID);
            if (unlocked)
            {
                hasAnyUnlocked = true;
                continue;
            }

            missing.Add(string.IsNullOrWhiteSpace(prereq.techName) ? prereq.techID : prereq.techName);
        }

        missingPrereqNames = string.Join(", ", missing);

        if (tech.prerequisiteMode == PrerequisiteMode.Any)
        {
            return hasAnyUnlocked;
        }

        return missing.Count == 0;
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

        CurrentTechPoints += amount;

        if (saveProgressOnChange)
        {
            SaveProgress();
        }

        OnTechStateChanged?.Invoke();
    }

    public IReadOnlyList<TechNodeSO> GetAllTechNodes()
    {
        if (techTree != null && techTree.allTechNodes != null && techTree.allTechNodes.Count > 0)
        {
            return techTree.allTechNodes;
        }

        return allTechNodes;
    }

    public IReadOnlyList<TechNodeSO> GetStartingUnlockedTechs()
    {
        if (techTree != null && techTree.startingUnlockedTechs != null && techTree.startingUnlockedTechs.Count > 0)
        {
            return techTree.startingUnlockedTechs;
        }

        return startingUnlockedTechs;
    }

    public void SaveProgress()
    {
        var data = new TechProgressData
        {
            availableTechPoints = CurrentTechPoints,
            unlockedTechIDs = new List<string>(unlockedTechIDs)
        };

        var json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public bool LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            return false;
        }

        var json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        var data = JsonUtility.FromJson<TechProgressData>(json);
        if (data == null)
        {
            return false;
        }

        CurrentTechPoints = data.availableTechPoints;
        unlockedTechIDs = data.unlockedTechIDs != null
            ? new HashSet<string>(data.unlockedTechIDs)
            : new HashSet<string>();

        return true;
    }

    public void ResetProgress()
    {
        InitializeStartingTechs();

        if (saveProgressOnChange)
        {
            SaveProgress();
        }

        OnTechStateChanged?.Invoke();
    }

    [ContextMenu("Validate Tech Tree")]
    public void ValidateTechTree()
    {
        var idLookup = new HashSet<string>();
        var nodeById = new Dictionary<string, TechNodeSO>();

        foreach (var node in GetAllTechNodes())
        {
            if (node == null)
            {
                Debug.LogWarning("Tech tree contains a null node reference.", this);
                continue;
            }

            if (string.IsNullOrWhiteSpace(node.techID))
            {
                Debug.LogWarning($"Tech node '{node.name}' has an empty techID.", node);
                continue;
            }

            if (!idLookup.Add(node.techID))
            {
                Debug.LogWarning($"Duplicate techID found: '{node.techID}'.", node);
                continue;
            }

            nodeById[node.techID] = node;
        }

        foreach (var node in GetAllTechNodes())
        {
            if (node == null || node.prerequisites == null)
            {
                continue;
            }

            foreach (var prereq in node.prerequisites)
            {
                if (prereq == null || string.IsNullOrWhiteSpace(prereq.techID))
                {
                    Debug.LogWarning($"Tech '{node.techName}' has an invalid prerequisite reference.", node);
                    continue;
                }

                if (prereq == node)
                {
                    Debug.LogWarning($"Tech '{node.techName}' cannot require itself.", node);
                    continue;
                }

                if (!nodeById.ContainsKey(prereq.techID))
                {
                    Debug.LogWarning($"Tech '{node.techName}' references prerequisite '{prereq.techID}' that is not in allTechNodes.", node);
                }
            }
        }

        foreach (var startNode in GetStartingUnlockedTechs())
        {
            if (startNode == null || string.IsNullOrWhiteSpace(startNode.techID))
            {
                Debug.LogWarning("Starting unlocked tech contains an invalid reference.", this);
                continue;
            }

            if (!nodeById.ContainsKey(startNode.techID))
            {
                Debug.LogWarning($"Starting unlocked tech '{startNode.techID}' is not listed in allTechNodes.", this);
            }
        }

        var visitation = new Dictionary<string, int>();
        foreach (var node in GetAllTechNodes())
        {
            if (node == null || string.IsNullOrWhiteSpace(node.techID))
            {
                continue;
            }

            if (HasCycle(node, visitation))
            {
                Debug.LogWarning($"Cycle detected involving tech '{node.techID}'.", node);
                break;
            }
        }
    }

    private bool HasCycle(TechNodeSO node, Dictionary<string, int> visitation)
    {
        if (node == null || string.IsNullOrWhiteSpace(node.techID))
        {
            return false;
        }

        if (visitation.TryGetValue(node.techID, out var state))
        {
            return state == 1;
        }

        visitation[node.techID] = 1;
        if (node.prerequisites != null)
        {
            foreach (var prereq in node.prerequisites)
            {
                if (HasCycle(prereq, visitation))
                {
                    return true;
                }
            }
        }

        visitation[node.techID] = 2;
        return false;
    }
}