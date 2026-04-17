using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public enum TechNodeLayoutMode
{
    TierRows,
    DesignerPositions
}

public class TechUIUpdater : MonoBehaviour
{
    [Header("References")]
    public TechManager techManager;
    public TechNodeUI techNodePrefab;
    public Transform nodeParent;

    [Header("Layout")]
    public TechNodeLayoutMode layoutMode = TechNodeLayoutMode.DesignerPositions;
    [Min(0f)] public float verticalSpacing = 120f;
    [Min(0f)] public float tierHorizontalSpacing = 260f;
    [Min(0f)] public float tierVerticalSpacing = 140f;
    public bool invertDesignerY = true;

    private readonly List<TechNodeUI> spawnedNodes = new();

    private void Start()
    {
        if (techManager == null)
        {
            techManager = FindAnyObjectByType<TechManager>();
        }

        BuildTreeUI();
        RefreshAllNodes();
    }

    private void OnEnable()
    {
        TechManager.OnTechUnlocked += HandleTechUnlocked;
        TechManager.OnTechStateChanged += RefreshAllNodes;
    }

    private void OnDisable()
    {
        TechManager.OnTechUnlocked -= HandleTechUnlocked;
        TechManager.OnTechStateChanged -= RefreshAllNodes;
    }

    private void BuildTreeUI()
    {
        if (techManager == null || techNodePrefab == null || nodeParent == null)
        {
            Debug.LogWarning("TechUIUpdater is missing references (TechManager, TechNodePrefab, or NodeParent).", this);
            return;
        }

        foreach (var nodeUI in spawnedNodes)
        {
            if (nodeUI != null)
            {
                Destroy(nodeUI.gameObject);
            }
        }
        spawnedNodes.Clear();

        var techNodes = techManager.GetAllTechNodes();
        if (techNodes == null || techNodes.Count == 0)
        {
            Debug.LogWarning("No tech nodes found. Assign TechTreeSO with nodes or populate TechManager.allTechNodes.", techManager);
            return;
        }

        foreach (var tech in techNodes)
        {
            if (tech == null)
            {
                continue;
            }

            var nodeInstance = Instantiate(techNodePrefab, nodeParent);
            nodeInstance.name = $"TechNode_{tech.techID}";
            nodeInstance.Initialize(tech, techManager);
            spawnedNodes.Add(nodeInstance);
        }

        if (spawnedNodes.Count == 0)
        {
            Debug.LogWarning("Tech node list contained only null entries.", techManager);
        }

        ApplyLayout(techNodes);
    }

    private void ApplyLayout(IReadOnlyList<TechNodeSO> techNodes)
    {
        if (nodeParent == null)
        {
            return;
        }

        var layoutGroup = nodeParent.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            Debug.LogWarning("Node parent has a LayoutGroup. It will override manual tech node positions.", layoutGroup);
        }

        switch (layoutMode)
        {
            case TechNodeLayoutMode.TierRows:
                ApplyTierLayout(techNodes);
                break;
            case TechNodeLayoutMode.DesignerPositions:
                ApplyDesignerPositionLayout(techNodes);
                break;
        }
    }

    private void ApplyTierLayout(IReadOnlyList<TechNodeSO> techNodes)
    {
        var rowByTier = new Dictionary<int, int>();

        int nodeIndex = 0;
        foreach (var tech in techNodes)
        {
            if (tech == null || nodeIndex >= spawnedNodes.Count)
            {
                continue;
            }

            if (!TryGetRectTransform(spawnedNodes[nodeIndex], out var rectTransform))
            {
                nodeIndex++;
                continue;
            }

            var tier = Mathf.Max(0, tech.tier);
            if (!rowByTier.TryGetValue(tier, out var row))
            {
                row = 0;
            }

            var x = tier * tierHorizontalSpacing;
            var y = -row * tierVerticalSpacing;
            rectTransform.anchoredPosition = new Vector2(x, y);

            rowByTier[tier] = row + 1;
            nodeIndex++;
        }
    }

    private void ApplyDesignerPositionLayout(IReadOnlyList<TechNodeSO> techNodes)
    {
        int nodeIndex = 0;
        foreach (var tech in techNodes)
        {
            if (tech == null || nodeIndex >= spawnedNodes.Count)
            {
                continue;
            }

            if (!TryGetRectTransform(spawnedNodes[nodeIndex], out var rectTransform))
            {
                nodeIndex++;
                continue;
            }

            var position = tech.editorPosition;
            if (invertDesignerY)
            {
                position.y = -position.y;
            }

            rectTransform.anchoredPosition = position;
            nodeIndex++;
        }
    }

    private bool TryGetRectTransform(TechNodeUI nodeUI, out RectTransform rectTransform)
    {
        rectTransform = null;
        if (nodeUI == null)
        {
            return false;
        }

        rectTransform = nodeUI.transform as RectTransform;
        return rectTransform != null;
    }

    private void RefreshAllNodes()
    {
        foreach (var nodeUI in spawnedNodes)
        {
            if (nodeUI != null)
            {
                nodeUI.UpdateVisualState();
            }
        }
    }

    private void HandleTechUnlocked(TechNodeSO newlyUnlockedTech)
    {
        RefreshAllNodes();
        Debug.Log($"Tech unlocked: {newlyUnlockedTech.techName}");
    }
}