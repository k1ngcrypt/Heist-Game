using System;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    [SerializeField]
    public LayerMask Obstacles;

    [SerializeField]
    private Vector2 gridWorldSize = new(20f, 20f);

    [SerializeField]
    private float nodeRadius = 0.5f;

    [SerializeField]
    private bool allowDiagonals = false;

    [SerializeField]
    private int clusterSize = 4;

    [SerializeField]
    private LayerMask specialTileMask;

    private float nodeDiameter;
    private int gridSizeX;
    private int gridSizeY;
    private int clusterCountX;
    private int clusterCountY;
    private HaNode[] nodes;
    private readonly List<HaCluster> clusters = new();
    private readonly List<HaPortal> portals = new();
    private readonly List<HaAbstractNode> abstractNodes = new();
    private int[] abstractNodeToGridIndex;
    private List<int>[] clusterAbstractNodes;
    private int[] portalAbstractA;
    private int[] portalAbstractB;
    private readonly Dictionary<long, int> lowLevelCostCache = new();

    private int[] gCosts;
    private int[] hCosts;
    private int[] parents;
    private bool[] closedFlags;
    private bool[] openFlags;
    private readonly List<int> openSet = new();
    private readonly List<int> abstractOpenSet = new();
    private int[] abstractGCosts;
    private int[] abstractHCosts;
    private int[] abstractParents;
    private bool[] abstractClosedFlags;
    private bool[] abstractOpenFlags;
    private readonly List<int> abstractNeighborBuffer = new();

    private void Awake()
    {
        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.x / nodeDiameter));
        gridSizeY = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.y / nodeDiameter));
        CreateGrid();
    }

    public List<Vector2> FindPath(Vector2 startPos, Vector2 targetPos)
    {
        List<int> pathIndices = FindPathIndices(startPos, targetPos);
        return ConvertPathToWorldPositions(pathIndices);
    }

    public List<int> FindPathIndices(Vector2 startPos, Vector2 targetPos)
    {
        if (nodes == null || nodes.Length == 0)
        {
            CreateGrid();
        }

        int startIndex = NodeIndexFromWorldPoint(startPos);
        int targetIndex = NodeIndexFromWorldPoint(targetPos);

        if (startIndex < 0 || targetIndex < 0)
        {
            return new List<int>();
        }

        if (!nodes[startIndex].IsTraversable() || !nodes[targetIndex].IsTraversable())
        {
            return new List<int>();
        }

        int startCluster = nodes[startIndex].ClusterId;
        int endCluster = nodes[targetIndex].ClusterId;

        if (startCluster == endCluster)
        {
            return FindLowLevelPathIndices(startIndex, targetIndex);
        }

        List<int> abstractPath = FindAbstractPath(startIndex, targetIndex);
        if (abstractPath.Count == 0)
        {
            return new List<int>();
        }

        List<int> finalPath = new();
        List<int> waypointNodes = BuildWaypointNodes(startIndex, targetIndex, abstractPath);
        for (int i = 0; i < waypointNodes.Count - 1; i++)
        {
            List<int> segment = FindLowLevelPathIndices(waypointNodes[i], waypointNodes[i + 1]);
            if (segment.Count == 0)
            {
                return new List<int>();
            }

            if (finalPath.Count > 0)
            {
                segment.RemoveAt(0);
            }

            finalPath.AddRange(segment);
        }

        return finalPath;
    }

    public int GetNodeIndexFromWorld(Vector2 worldPosition)
    {
        if (nodes == null || nodes.Length == 0)
        {
            CreateGrid();
        }

        return NodeIndexFromWorldPoint(worldPosition);
    }

    public Vector2 GetNodeWorldPosition(int index)
    {
        if (nodes == null || index < 0 || index >= nodes.Length)
        {
            return Vector2.zero;
        }

        return nodes[index].WorldPosition;
    }

    public bool TryGetNodeInteraction(int index, out ISpecialTile interaction)
    {
        interaction = null;
        if (nodes == null || index < 0 || index >= nodes.Length)
        {
            return false;
        }

        interaction = nodes[index].Interaction;
        return interaction != null;
    }

    private void CreateGrid()
    {
        nodes = new HaNode[gridSizeX * gridSizeY];
        Vector2 worldBottomLeft = (Vector2)transform.position - Vector2.right * gridWorldSize.x / 2f - Vector2.up * gridWorldSize.y / 2f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector2 worldPoint = worldBottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius)
                                                   + Vector2.up * (y * nodeDiameter + nodeRadius);
                bool walkable = !Physics2D.OverlapCircle(worldPoint, nodeRadius, Obstacles);
                ISpecialTile interaction = null;
                if (specialTileMask != 0)
                {
                    Collider2D specialCollider = Physics2D.OverlapCircle(worldPoint, nodeRadius, specialTileMask);
                    if (specialCollider != null)
                    {
                        interaction = specialCollider.GetComponentInParent<ISpecialTile>() ?? specialCollider.GetComponent<ISpecialTile>();
                    }
                }

                if (interaction != null)
                {
                    walkable = true;
                }

                int index = GetIndex(x, y);
                nodes[index] = new HaNode
                {
                    GridPosition = new Vector2Int(x, y),
                    WorldPosition = worldPoint,
                    Walkable = walkable,
                    GCost = 0,
                    HCost = 0,
                    ParentIndex = -1,
                    Neighbors = Array.Empty<int>(),
                    ClusterId = 0,
                    Flags = walkable ? HaNodeFlags.Walkable : HaNodeFlags.None,
                    Interaction = interaction
                };
            }
        }

        BuildNeighbors();
        BuildClustersAndPortals();
        EnsureSearchArrays();
    }

    private void BuildNeighbors()
    {
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                List<int> neighborIndices = new();
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                        {
                            continue;
                        }

                        if (!allowDiagonals && Mathf.Abs(offsetX) + Mathf.Abs(offsetY) > 1)
                        {
                            continue;
                        }

                        int checkX = x + offsetX;
                        int checkY = y + offsetY;

                        if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                        {
                            neighborIndices.Add(GetIndex(checkX, checkY));
                        }
                    }
                }

                int index = GetIndex(x, y);
                HaNode node = nodes[index];
                node.Neighbors = neighborIndices.ToArray();
                nodes[index] = node;
            }
        }
    }

    private void BuildClustersAndPortals()
    {
        clusters.Clear();
        portals.Clear();
        abstractNodes.Clear();
        lowLevelCostCache.Clear();

        clusterCountX = Mathf.Max(1, Mathf.CeilToInt(gridSizeX / (float)clusterSize));
        clusterCountY = Mathf.Max(1, Mathf.CeilToInt(gridSizeY / (float)clusterSize));
        int clusterCount = clusterCountX * clusterCountY;
        for (int i = 0; i < clusterCount; i++)
        {
            clusters.Add(new HaCluster(i));
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                int index = GetIndex(x, y);
                int clusterId = GetClusterId(x, y);
                HaNode node = nodes[index];
                node.ClusterId = clusterId;
                nodes[index] = node;
                clusters[clusterId].NodeIndices.Add(index);
            }
        }

        for (int index = 0; index < nodes.Length; index++)
        {
            if (!nodes[index].IsTraversable())
            {
                continue;
            }

            HaNode node = nodes[index];
            foreach (int neighborIndex in node.Neighbors)
            {
                if (neighborIndex <= index)
                {
                    continue;
                }

                if (!nodes[neighborIndex].IsTraversable())
                {
                    continue;
                }

                int clusterA = node.ClusterId;
                int clusterB = nodes[neighborIndex].ClusterId;
                if (clusterA == clusterB)
                {
                    continue;
                }

                int portalIndex = portals.Count;
                portals.Add(new HaPortal
                {
                    FromNodeIndex = index,
                    ToNodeIndex = neighborIndex,
                    Cost = GetDistance(index, neighborIndex)
                });

                clusters[clusterA].PortalIndices.Add(portalIndex);
                clusters[clusterB].PortalIndices.Add(portalIndex);

                int abstractIndexA = abstractNodes.Count;
                abstractNodes.Add(new HaAbstractNode
                {
                    PortalIndex = portalIndex,
                    ClusterId = clusterA,
                    GCost = 0,
                    HCost = 0,
                    ParentIndex = -1,
                    Neighbors = Array.Empty<int>()
                });

                int abstractIndexB = abstractNodes.Count;
                abstractNodes.Add(new HaAbstractNode
                {
                    PortalIndex = portalIndex,
                    ClusterId = clusterB,
                    GCost = 0,
                    HCost = 0,
                    ParentIndex = -1,
                    Neighbors = Array.Empty<int>()
                });

                AddAbstractMapping(abstractIndexA, index, clusterA, portalIndex, abstractIndexB, neighborIndex, clusterB);
            }
        }

        BuildAbstractNeighbors();
    }

    private void AddAbstractMapping(int abstractIndexA, int gridIndexA, int clusterA, int portalIndex, int abstractIndexB, int gridIndexB, int clusterB)
    {
        if (abstractNodeToGridIndex == null || abstractNodeToGridIndex.Length < abstractNodes.Count)
        {
            Array.Resize(ref abstractNodeToGridIndex, abstractNodes.Count);
        }

        if (clusterAbstractNodes == null || clusterAbstractNodes.Length != clusters.Count)
        {
            clusterAbstractNodes = new List<int>[clusters.Count];
            for (int i = 0; i < clusters.Count; i++)
            {
                clusterAbstractNodes[i] = new List<int>();
            }
        }

        if (portalAbstractA == null || portalAbstractA.Length < portals.Count)
        {
            Array.Resize(ref portalAbstractA, portals.Count);
            Array.Resize(ref portalAbstractB, portals.Count);
        }

        abstractNodeToGridIndex[abstractIndexA] = gridIndexA;
        abstractNodeToGridIndex[abstractIndexB] = gridIndexB;
        clusterAbstractNodes[clusterA].Add(abstractIndexA);
        clusterAbstractNodes[clusterB].Add(abstractIndexB);
        portalAbstractA[portalIndex] = abstractIndexA;
        portalAbstractB[portalIndex] = abstractIndexB;
    }

    private void BuildAbstractNeighbors()
    {
        for (int i = 0; i < abstractNodes.Count; i++)
        {
            HaAbstractNode node = abstractNodes[i];
            List<int> neighbors = new();

            int portalIndex = node.PortalIndex;
            int paired = portalAbstractA[portalIndex] == i ? portalAbstractB[portalIndex] : portalAbstractA[portalIndex];
            if (paired >= 0)
            {
                neighbors.Add(paired);
            }

            List<int> clusterNodes = clusterAbstractNodes[node.ClusterId];
            for (int j = 0; j < clusterNodes.Count; j++)
            {
                int candidate = clusterNodes[j];
                if (candidate != i)
                {
                    neighbors.Add(candidate);
                }
            }

            node.Neighbors = neighbors.ToArray();
            abstractNodes[i] = node;
        }
    }

    private int NodeIndexFromWorldPoint(Vector2 worldPosition)
    {
        Vector2 worldBottomLeft = (Vector2)transform.position - Vector2.right * gridWorldSize.x / 2f - Vector2.up * gridWorldSize.y / 2f;
        float percentX = Mathf.Clamp01((worldPosition.x - worldBottomLeft.x) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPosition.y - worldBottomLeft.y) / gridWorldSize.y);

        int x = Mathf.Clamp(Mathf.RoundToInt((gridSizeX - 1) * percentX), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((gridSizeY - 1) * percentY), 0, gridSizeY - 1);

        return GetIndex(x, y);
    }

    private int GetIndex(int x, int y) => x + y * gridSizeX;

    private int GetClusterId(int x, int y)
    {
        int clusterX = Mathf.Clamp(x / clusterSize, 0, clusterCountX - 1);
        int clusterY = Mathf.Clamp(y / clusterSize, 0, clusterCountY - 1);
        return clusterX + clusterY * clusterCountX;
    }

    private List<int> FindLowLevelPathIndices(int startIndex, int endIndex)
    {
        if (startIndex == endIndex)
        {
            return new List<int> { startIndex };
        }

        EnsureSearchArrays();
        ResetSearchArrays();
        openSet.Clear();

        gCosts[startIndex] = 0;
        hCosts[startIndex] = GetDistance(startIndex, endIndex);
        openSet.Add(startIndex);
        openFlags[startIndex] = true;

        while (openSet.Count > 0)
        {
            int currentIndex = GetLowestFCostIndex(openSet, gCosts, hCosts);
            openSet.Remove(currentIndex);
            openFlags[currentIndex] = false;
            closedFlags[currentIndex] = true;

            if (currentIndex == endIndex)
            {
                return RetracePathIndices(startIndex, endIndex);
            }

            foreach (int neighborIndex in nodes[currentIndex].Neighbors)
            {
                if (closedFlags[neighborIndex])
                {
                    continue;
                }

                if (!nodes[neighborIndex].IsTraversable())
                {
                    continue;
                }

                int newMovementCost = gCosts[currentIndex] + GetDistance(currentIndex, neighborIndex);
                if (newMovementCost < gCosts[neighborIndex] || !openFlags[neighborIndex])
                {
                    gCosts[neighborIndex] = newMovementCost;
                    hCosts[neighborIndex] = GetDistance(neighborIndex, endIndex);
                    parents[neighborIndex] = currentIndex;

                    if (!openFlags[neighborIndex])
                    {
                        openSet.Add(neighborIndex);
                        openFlags[neighborIndex] = true;
                    }
                }
            }
        }

        return new List<int>();
    }

    private int FindLowLevelCost(int startIndex, int endIndex)
    {
        if (startIndex == endIndex)
        {
            return 0;
        }

        EnsureSearchArrays();
        ResetSearchArrays();
        openSet.Clear();

        gCosts[startIndex] = 0;
        hCosts[startIndex] = GetDistance(startIndex, endIndex);
        openSet.Add(startIndex);
        openFlags[startIndex] = true;

        while (openSet.Count > 0)
        {
            int currentIndex = GetLowestFCostIndex(openSet, gCosts, hCosts);
            openSet.Remove(currentIndex);
            openFlags[currentIndex] = false;
            closedFlags[currentIndex] = true;

            if (currentIndex == endIndex)
            {
                return gCosts[endIndex];
            }

            foreach (int neighborIndex in nodes[currentIndex].Neighbors)
            {
                if (closedFlags[neighborIndex])
                {
                    continue;
                }

                if (!nodes[neighborIndex].IsTraversable())
                {
                    continue;
                }

                int newMovementCost = gCosts[currentIndex] + GetDistance(currentIndex, neighborIndex);
                if (newMovementCost < gCosts[neighborIndex] || !openFlags[neighborIndex])
                {
                    gCosts[neighborIndex] = newMovementCost;
                    hCosts[neighborIndex] = GetDistance(neighborIndex, endIndex);
                    parents[neighborIndex] = currentIndex;

                    if (!openFlags[neighborIndex])
                    {
                        openSet.Add(neighborIndex);
                        openFlags[neighborIndex] = true;
                    }
                }
            }
        }

        return int.MaxValue;
    }

    private List<int> RetracePathIndices(int startIndex, int endIndex)
    {
        List<int> path = new();
        int currentIndex = endIndex;

        while (currentIndex != startIndex)
        {
            path.Add(currentIndex);
            currentIndex = parents[currentIndex];
            if (currentIndex < 0)
            {
                return new List<int>();
            }
        }

        path.Add(startIndex);
        path.Reverse();
        return path;
    }

    private List<int> FindAbstractPath(int startIndex, int endIndex)
    {
        int startCluster = nodes[startIndex].ClusterId;
        int endCluster = nodes[endIndex].ClusterId;
        int totalNodes = abstractNodes.Count + 2;
        int virtualStart = abstractNodes.Count;
        int virtualEnd = abstractNodes.Count + 1;

        if (!EnsureAbstractSearchArrays(totalNodes))
        {
            return new List<int>();
        }

        ResetAbstractSearchArrays(totalNodes);
        abstractOpenSet.Clear();

        abstractGCosts[virtualStart] = 0;
        abstractHCosts[virtualStart] = GetDistance(startIndex, endIndex);
        abstractOpenSet.Add(virtualStart);
        abstractOpenFlags[virtualStart] = true;

        while (abstractOpenSet.Count > 0)
        {
            int currentIndex = GetLowestFCostIndex(abstractOpenSet, abstractGCosts, abstractHCosts);
            abstractOpenSet.Remove(currentIndex);
            abstractOpenFlags[currentIndex] = false;
            abstractClosedFlags[currentIndex] = true;

            if (currentIndex == virtualEnd)
            {
                return RetraceAbstractPath(virtualStart, virtualEnd);
            }

            EnumerateAbstractNeighbors(currentIndex, startIndex, endIndex, startCluster, endCluster);
            foreach (int neighborIndex in abstractNeighborBuffer)
            {
                if (abstractClosedFlags[neighborIndex])
                {
                    continue;
                }

                int cost = GetAbstractTransitionCost(currentIndex, neighborIndex, startIndex, endIndex, virtualStart, virtualEnd);
                if (cost == int.MaxValue)
                {
                    continue;
                }

                int newMovementCost = abstractGCosts[currentIndex] + cost;
                if (newMovementCost < abstractGCosts[neighborIndex] || !abstractOpenFlags[neighborIndex])
                {
                    abstractGCosts[neighborIndex] = newMovementCost;
                    abstractHCosts[neighborIndex] = GetDistance(GetAbstractNodeGridIndex(neighborIndex, startIndex, endIndex), endIndex);
                    abstractParents[neighborIndex] = currentIndex;

                    if (!abstractOpenFlags[neighborIndex])
                    {
                        abstractOpenSet.Add(neighborIndex);
                        abstractOpenFlags[neighborIndex] = true;
                    }
                }
            }
        }

        return new List<int>();
    }

    private void EnumerateAbstractNeighbors(int nodeIndex, int startIndex, int endIndex, int startCluster, int endCluster)
    {
        abstractNeighborBuffer.Clear();
        int virtualStart = abstractNodes.Count;
        int virtualEnd = abstractNodes.Count + 1;

        if (nodeIndex == virtualStart)
        {
            abstractNeighborBuffer.AddRange(clusterAbstractNodes[startCluster]);
            return;
        }

        if (nodeIndex == virtualEnd)
        {
            return;
        }

        HaAbstractNode node = abstractNodes[nodeIndex];
        abstractNeighborBuffer.AddRange(node.Neighbors);
        if (node.ClusterId == endCluster)
        {
            abstractNeighborBuffer.Add(virtualEnd);
        }
    }

    private int GetAbstractTransitionCost(int fromIndex, int toIndex, int startIndex, int endIndex, int virtualStart, int virtualEnd)
    {
        if (fromIndex == virtualStart)
        {
            return GetCachedLowLevelCost(startIndex, GetAbstractNodeGridIndex(toIndex, startIndex, endIndex));
        }

        if (toIndex == virtualEnd)
        {
            return GetCachedLowLevelCost(GetAbstractNodeGridIndex(fromIndex, startIndex, endIndex), endIndex);
        }

        if (fromIndex >= abstractNodes.Count || toIndex >= abstractNodes.Count)
        {
            return int.MaxValue;
        }

        HaAbstractNode fromNode = abstractNodes[fromIndex];
        HaAbstractNode toNode = abstractNodes[toIndex];
        if (fromNode.PortalIndex == toNode.PortalIndex && fromNode.ClusterId != toNode.ClusterId)
        {
            return portals[fromNode.PortalIndex].Cost;
        }

        return GetCachedLowLevelCost(abstractNodeToGridIndex[fromIndex], abstractNodeToGridIndex[toIndex]);
    }

    private int GetAbstractNodeGridIndex(int abstractIndex, int startIndex, int endIndex)
    {
        if (abstractIndex == abstractNodes.Count)
        {
            return startIndex;
        }

        if (abstractIndex == abstractNodes.Count + 1)
        {
            return endIndex;
        }

        return abstractNodeToGridIndex[abstractIndex];
    }

    private List<int> RetraceAbstractPath(int startIndex, int endIndex)
    {
        List<int> path = new();
        int currentIndex = endIndex;

        while (currentIndex != startIndex)
        {
            path.Add(currentIndex);
            currentIndex = abstractParents[currentIndex];
            if (currentIndex < 0)
            {
                return new List<int>();
            }
        }

        path.Add(startIndex);
        path.Reverse();
        return path;
    }

    private List<int> BuildWaypointNodes(int startIndex, int endIndex, List<int> abstractPath)
    {
        List<int> waypoints = new()
        {
            startIndex
        };
        int virtualStart = abstractNodes.Count;
        int virtualEnd = abstractNodes.Count + 1;

        for (int i = 0; i < abstractPath.Count; i++)
        {
            int abstractIndex = abstractPath[i];
            if (abstractIndex == virtualStart || abstractIndex == virtualEnd)
            {
                continue;
            }

            waypoints.Add(abstractNodeToGridIndex[abstractIndex]);
        }

        waypoints.Add(endIndex);
        return waypoints;
    }

    private List<Vector2> ConvertPathToWorldPositions(List<int> nodeIndices)
    {
        List<Vector2> path = new();
        for (int i = 0; i < nodeIndices.Count; i++)
        {
            path.Add(nodes[nodeIndices[i]].WorldPosition);
        }

        return path;
    }

    private int GetLowestFCostIndex(List<int> indices, int[] gCost, int[] hCost)
    {
        int bestIndex = indices[0];
        int bestCost = gCost[bestIndex] + hCost[bestIndex];
        int bestHCost = hCost[bestIndex];

        for (int i = 1; i < indices.Count; i++)
        {
            int candidate = indices[i];
            int candidateCost = gCost[candidate] + hCost[candidate];
            if (candidateCost < bestCost || (candidateCost == bestCost && hCost[candidate] < bestHCost))
            {
                bestIndex = candidate;
                bestCost = candidateCost;
                bestHCost = hCost[candidate];
            }
        }

        return bestIndex;
    }

    private int GetDistance(int indexA, int indexB)
    {
        Vector2Int a = nodes[indexA].GridPosition;
        Vector2Int b = nodes[indexB].GridPosition;
        int dstX = Mathf.Abs(a.x - b.x);
        int dstY = Mathf.Abs(a.y - b.y);

        if (allowDiagonals)
        {
            int remaining = Mathf.Abs(dstX - dstY);
            return 14 * Mathf.Min(dstX, dstY) + 10 * remaining;
        }

        return 10 * (dstX + dstY);
    }

    private void EnsureSearchArrays()
    {
        if (nodes == null)
        {
            return;
        }

        int length = nodes.Length;
        if (gCosts == null || gCosts.Length != length)
        {
            gCosts = new int[length];
            hCosts = new int[length];
            parents = new int[length];
            closedFlags = new bool[length];
            openFlags = new bool[length];
        }
    }

    private void ResetSearchArrays()
    {
        for (int i = 0; i < gCosts.Length; i++)
        {
            gCosts[i] = int.MaxValue;
            hCosts[i] = 0;
            parents[i] = -1;
            closedFlags[i] = false;
            openFlags[i] = false;
        }
    }

    private bool EnsureAbstractSearchArrays(int totalNodes)
    {
        if (totalNodes <= 0)
        {
            return false;
        }

        if (abstractGCosts == null || abstractGCosts.Length < totalNodes)
        {
            abstractGCosts = new int[totalNodes];
            abstractHCosts = new int[totalNodes];
            abstractParents = new int[totalNodes];
            abstractClosedFlags = new bool[totalNodes];
            abstractOpenFlags = new bool[totalNodes];
        }

        return true;
    }

    private void ResetAbstractSearchArrays(int totalNodes)
    {
        for (int i = 0; i < totalNodes; i++)
        {
            abstractGCosts[i] = int.MaxValue;
            abstractHCosts[i] = 0;
            abstractParents[i] = -1;
            abstractClosedFlags[i] = false;
            abstractOpenFlags[i] = false;
        }
    }

    private int GetCachedLowLevelCost(int startIndex, int endIndex)
    {
        if (startIndex == endIndex)
        {
            return 0;
        }

        int min = Mathf.Min(startIndex, endIndex);
        int max = Mathf.Max(startIndex, endIndex);
        long key = ((long)min << 32) | (uint)max;

        if (lowLevelCostCache.TryGetValue(key, out int cachedCost))
        {
            return cachedCost;
        }

        int cost = FindLowLevelCost(startIndex, endIndex);
        lowLevelCostCache[key] = cost;
        return cost;
    }
}
