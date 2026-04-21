using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class TechUIUpdater : MonoBehaviour
{
    [Header("References")]
    public TechManager techManager;
    public TechNodeUI techNodePrefab;
    public Transform nodeParent;

    [Header("Layout")]
    [Min(0f)] public float tierHorizontalSpacing = 260f;
    [Min(0f)] public float tierVerticalSpacing = 140f;
    public bool branchDown = true;

    int branchDirection = 0;

    private readonly List<TechNodeUI> spawnedNodes = new();

    private void Start()
    {
        if (techManager == null)
        {
            techManager = FindAnyObjectByType<TechManager>();
        }
        if (branchDown)
        {
            branchDirection = -1;
        } else
        {
            branchDirection = 1;
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

        var nodesByCategorySO = new Dictionary<string, List<TechNodeSO>>();
        var nodesByCategoryUI = new Dictionary<string, List<TechNodeUI>>();
        var categoryParents = new Dictionary<string, RectTransform>();

        foreach (var tech in techNodes)
        {
            if (tech == null)
            {
                continue;
            }
            TechNodeUI nodeInstance; 

            if (tech.tier == 0 || tech.category == "Base")
            {
                if (!categoryParents.TryGetValue("Base", out var papa))
                {
                    GameObject container = new GameObject($"Category_Base", typeof(RectTransform));
                    container.transform.SetParent(nodeParent, false);

                    papa = container.GetComponent<RectTransform>();
                    categoryParents["Base"] = papa;
                }
                if (!nodesByCategorySO.ContainsKey("Base")) {
                    nodesByCategorySO["Base"] = new List<TechNodeSO>();
                    nodesByCategoryUI["Base"] = new List<TechNodeUI>();
                }
                nodesByCategorySO["Base"].Add(tech);

                nodeInstance = Instantiate(techNodePrefab, papa);
                nodeInstance.name = $"TechNode_{tech.techID}";
                nodeInstance.Initialize(tech, techManager);
                nodeInstance.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                spawnedNodes.Add(nodeInstance);
                nodesByCategoryUI["Base"].Add(nodeInstance);
            }
            else
            {
                //group into sub catagories
                if (!categoryParents.TryGetValue(tech.category, out var parent))
                {
                    GameObject container = new GameObject($"Category_{tech.category}", typeof(RectTransform));
                    container.transform.SetParent(nodeParent, false);

                    parent = container.GetComponent<RectTransform>();
                    categoryParents[tech.category] = parent;
                }
                if (!nodesByCategorySO.ContainsKey(tech.category)) {
                    nodesByCategorySO[tech.category] = new List<TechNodeSO>();
                    nodesByCategoryUI[tech.category] = new List<TechNodeUI>();
                }
                nodesByCategorySO[tech.category].Add(tech);

                nodeInstance = Instantiate(techNodePrefab, parent);
                nodeInstance.name = $"TechNode_{tech.techID}";
                nodeInstance.Initialize(tech, techManager);
                spawnedNodes.Add(nodeInstance);
                nodesByCategoryUI[tech.category].Add(nodeInstance);   
            }
        }

        if (spawnedNodes.Count == 0)
        {
            Debug.LogWarning("Tech node list contained only null entries.", techManager);
        }

        ApplyLayout(nodesByCategorySO, nodesByCategoryUI, categoryParents);
    }

    private void ApplyLayout(Dictionary<string, List<TechNodeSO>> nodesByCategorySO, Dictionary<string, List<TechNodeUI>> nodesByCategoryUI, Dictionary<string, RectTransform> categoryParents)
    {

        if (nodeParent == null)
        {
            return;
        }

        float nodeWidth = techNodePrefab.GetComponent<RectTransform>().rect.width;
        float nodeHeight = techNodePrefab.GetComponent<RectTransform>().rect.height;
        Vector2 nodeCorner = new Vector2(nodeWidth/2f, nodeHeight/2f);

        //organize each node in each category
        foreach (var kvp in nodesByCategorySO)
        {
            string category = kvp.Key;
            List<TechNodeSO> techItem = kvp.Value;
            List<TechNodeUI> techInstance = nodesByCategoryUI[category];
            
            RectTransform parent = categoryParents[category];
            parent.pivot = new Vector2(0.5f, 0.5f);

            int count = techItem.Count;

            int maxTier = 0;
            for (int i = 0; i < count; i++) {
                if (techItem[i] != null) {
                    maxTier = Mathf.Max(maxTier, techItem[i].tier);
                }
            }

            // Count nodes per tier
            int[] numOfRows = new int[maxTier + 1];
            for (int i = 0; i < count; i++) {
                var tech = techItem[i];
                if (tech == null) {
                    continue;
                }
                int tier = Mathf.Max(0, tech.tier);
                numOfRows[tier]++;
            }

            // Precompute offsets
            float[] offsetPerTier = new float[maxTier + 1];

            for (int t = 0; t <= maxTier; t++)
            {
                if (numOfRows[t] <= 1) {
                    offsetPerTier[t] = 0;
                }
                else {
                    offsetPerTier[t] = (((numOfRows[t] - 1) * (nodeWidth + tierHorizontalSpacing)) / 2f);
                }
            }

            // Track row index per tier
            int[] placementRow = new int[maxTier + 1];

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            // Place nodes
            for (int i = 0; i < count; i++)
            {
                var tech = techItem[i];
                if (tech == null) {
                    continue;
                }

                int tier = Mathf.Max(0, tech.tier);
                int row = placementRow[tier];

                

                float x = (row * (nodeWidth + tierHorizontalSpacing)) - offsetPerTier[tier];
                float y = 0;
                if (category != "Base")
                {
                    y = branchDirection * (tier - 1) * (tierVerticalSpacing + nodeHeight);
                }                

                Vector2 pos = new Vector2(x, y);

                min = Vector2.Min(min, pos - nodeCorner);
                max = Vector2.Max(max, pos + nodeCorner);

                techInstance[i].GetComponent<RectTransform>().anchoredPosition = pos;

                placementRow[tier]++;

                techInstance[i].GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1);
                techInstance[i].GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1);
                techInstance[i].GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
            }

            //resize parent
            parent.sizeDelta = (max - min);
        }

        //organize parents & resize content
        int numOfCategories = categoryParents.Count-1;
        float offset = 0;
        int c = 0;
        foreach (var kpv in categoryParents)
        {
            if (kpv.Key == "Base") {
                continue;
            }
            var parent = kpv.Value;
            if (c == 0 || c == numOfCategories - 1) {
                offset += ((parent.sizeDelta.x + tierHorizontalSpacing) / 2f);
            } else {
                offset += parent.sizeDelta.x + tierHorizontalSpacing;
            }
        }

        offset /= 2;

        float lastLocation = 0f;
        bool doneOnce = false;
        float lastWidth = 0f;
        Vector2 minCanvas = -nodeCorner;
        Vector2 maxCanvas = nodeCorner;
        foreach (var kpv in categoryParents)
        {
            if (kpv.Key == "Base") {
                continue;
            }
            var parent = kpv.Value;
            float width = parent.sizeDelta.x;
            float height = parent.sizeDelta.y;
            if (!doneOnce)
            {
                doneOnce = true;
                parent.anchoredPosition += new Vector2(-offset, branchDirection * (tierVerticalSpacing + ((nodeHeight + height)/2f)));
                lastLocation = -offset;
                lastWidth = width;
            } else 
            {
                lastLocation += (((lastWidth + width) / 2f) + tierHorizontalSpacing);
                parent.anchoredPosition = new Vector2(lastLocation, branchDirection * (tierVerticalSpacing + ((nodeHeight + height)/2f)));
                lastWidth = width;
            }

            //for content resize
            Vector2 pos = parent.anchoredPosition;
            Vector2 canvasCorner = new Vector2(width / 2f, height / 2f);
            
            // Update bounds
            maxCanvas = Vector2.Max(maxCanvas, pos + canvasCorner);
            minCanvas = Vector2.Min(minCanvas, pos - canvasCorner);

            parent.anchorMin = new Vector2(0.5f, 0.5f);
            parent.anchorMax = new Vector2(0.5f, 0.5f);
        }

        //resize content with padding
        var canvasRT = nodeParent.GetComponent<RectTransform>();
        Vector2 padding = new Vector2(((canvasRT.sizeDelta.x - nodeWidth) / 2f), ((canvasRT.sizeDelta.y - nodeHeight) / 2f));
        maxCanvas += padding;
        minCanvas -= padding;
        //resize contents
        canvasRT.pivot = new Vector2(0.5f, 1f);
        canvasRT.sizeDelta = (maxCanvas - minCanvas);

        //shift each parent to account for new size
        Vector2 center = (maxCanvas + minCanvas) / 2f;
        foreach (var kvp in categoryParents) {
            RectTransform rt = kvp.Value;
            rt.anchoredPosition += new Vector2(0, branchDirection * center.y);
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