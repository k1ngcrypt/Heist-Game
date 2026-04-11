using UnityEngine;
using System.Collections.Generic;

public class TechUIUpdater : MonoBehaviour
{
    [Header("References")]
    public TechManager techManager;
    public TechNodeUI techNodePrefab;
    public Transform nodeParent;

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

        foreach (var tech in techManager.allTechNodes)
        {
            if (tech == null)
            {
                continue;
            }

            var nodeInstance = Instantiate(techNodePrefab, nodeParent);
            nodeInstance.Initialize(tech, techManager);
            spawnedNodes.Add(nodeInstance);
        }
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