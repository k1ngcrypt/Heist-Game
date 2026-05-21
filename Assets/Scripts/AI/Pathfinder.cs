using System;
using System.Collections.Generic;
using HeistGame.Door;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinder : MonoBehaviour
{
    [SerializeField]
    private LayerMask Obstacles;

    [SerializeField]
    private LayerMask playerCollisionMask;

    [SerializeField]
    private Vector2 gridWorldSize = new(20f, 20f);

    [SerializeField]
    private bool autoSizeGridToTilemap = true;

    [SerializeField]
    private Vector2 gridWorldPadding = Vector2.zero;

    [SerializeField]
    private float nodeRadius = 0.5f;

    [SerializeField]
    private float collisionRadiusShrink = 0.05f;

    [SerializeField]
    private bool allowDiagonals = false;

    [SerializeField]
    private LayerMask specialTileMask;

    [Header("Room Settings")]
    [SerializeField]
    private Tilemap floorTilemap;

    [SerializeField]
    private List<TileBase> floorTiles = new();

    [SerializeField]
    private bool separateRoomsByTileType = true;

    [Header("Editor Build")]
    [SerializeField]
    private bool useCachedGridData = true;

    [SerializeField]
    private bool useCachedAbstractData = true;

    [SerializeField]
    private bool autoRebuildInEditor = true;

    [Header("Gizmos")]
    [SerializeField]
    private bool drawRoomGizmos = true;

    [SerializeField]
    private bool drawPortalGizmos = true;

    [SerializeField]
    private bool drawSpecialTileGizmos = true;

    [SerializeField]
    private bool drawGizmosWhenNotSelected = true;

    [SerializeField]
    private float gizmoNodeSize = 0.25f;

    [SerializeField]
    private Color portalGizmoColor = new(0.2f, 0.8f, 1f, 1f);

    [SerializeField]
    private Color specialLinkGizmoColor = new(1f, 0.4f, 1f, 1f);

    [SerializeField]
    private Color specialTileGizmoColor = new(1f, 0.85f, 0.2f, 1f);

    private float nodeDiameter;
    private int gridSizeX;
    private int gridSizeY;
    private Vector2 gridWorldBottomLeft;
    private HaNode[] nodes;
    [SerializeField, HideInInspector]
    private List<HaRoom> rooms = new();

    [SerializeField, HideInInspector]
    private List<HaPortal> portals = new();

    [SerializeField, HideInInspector]
    private List<HaAbstractNode> abstractNodes = new();

    [SerializeField, HideInInspector]
    private int[] cachedRoomIds;

    [SerializeField, HideInInspector]
    private int cachedGridSizeX;

    [SerializeField, HideInInspector]
    private int cachedGridSizeY;

    [SerializeField, HideInInspector]
    private Vector2 cachedGridWorldSize;

    [SerializeField, HideInInspector]
    private Vector2 cachedGridWorldBottomLeft;

    [SerializeField, HideInInspector]
    private float cachedNodeRadius;

    [SerializeField, HideInInspector]
    private bool[] cachedWalkable;

    [SerializeField, HideInInspector]
    private List<CachedSpecialTileEntry> cachedSpecialTiles = new();

    [SerializeField, HideInInspector]
    private List<RoomAbstractNodeList> cachedRoomAbstractNodes = new();

    [SerializeField, HideInInspector]
    private List<PortalCostEntry> cachedRoomPortalCosts = new();
    [SerializeField, HideInInspector]
    private int[] abstractNodeToGridIndex;

    private List<int>[] roomAbstractNodes;

    [SerializeField, HideInInspector]
    private int[] portalAbstractA;

    [SerializeField, HideInInspector]
    private int[] portalAbstractB;
    private readonly Dictionary<long, int> lowLevelCostCache = new();
    private readonly Dictionary<long, int> roomPortalCostCache = new();
    private readonly Dictionary<(int roomId, int min, int max), int> roomLowLevelCostCache = new();

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
    private static readonly List<Pathfinder> ActivePathfinders = new();

    private void Awake()
    {
        CreateGrid(true);
    }

    private void OnEnable()
    {
        if (!ActivePathfinders.Contains(this))
        {
            ActivePathfinders.Add(this);
        }
    }

    private void OnDisable()
    {
        ActivePathfinders.Remove(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !autoRebuildInEditor)
        {
            return;
        }

        RebuildAbstractGraphInEditor();
    }

    [ContextMenu("Rebuild Abstract Graph (Editor)")]
    private void RebuildAbstractGraphInEditor()
    {
        if (Application.isPlaying)
        {
            return;
        }

        BuildGrid();
        BuildRoomsAndPortals(true);
    }
#endif

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

        int startRoom = nodes[startIndex].RoomId;
        int endRoom = nodes[targetIndex].RoomId;

        if (startRoom < 0 || endRoom < 0)
        {
            return new List<int>();
        }

        if (startRoom == endRoom)
        {
            return FindLowLevelPathIndicesInRoom(startIndex, targetIndex, startRoom);
        }

        List<int> abstractPath = FindAbstractPath(startIndex, targetIndex);
        if (abstractPath.Count == 0)
        {
            return new List<int>();
        }

        return BuildFinalPathFromAbstract(startIndex, targetIndex, abstractPath);
    }

    private List<int> FindLowLevelPathIndicesInRoom(int startIndex, int endIndex, int roomId)
    {
        if (roomId < 0 || nodes[startIndex].RoomId != roomId || nodes[endIndex].RoomId != roomId)
        {
            return new List<int>();
        }

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

                if (!nodes[neighborIndex].IsTraversable() || nodes[neighborIndex].RoomId != roomId)
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

    private void OnDrawGizmos()
    {
        if (!drawGizmosWhenNotSelected)
        {
            return;
        }

        DrawGizmosInternal();
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmosInternal();
    }

    private void DrawGizmosInternal()
    {
        EnsureGizmoData();
        if (nodes == null || nodes.Length == 0)
        {
            return;
        }

        if (drawRoomGizmos)
        {
            DrawRoomGizmos();
        }

        if (drawPortalGizmos)
        {
            DrawPortalGizmos();
        }

        if (drawSpecialTileGizmos)
        {
            DrawSpecialTileGizmos();
        }
    }

    private void EnsureGizmoData()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (nodes != null && nodes.Length > 0)
        {
            return;
        }

        CreateGrid(true);
    }

    private void DrawRoomGizmos()
    {
        if (rooms == null)
        {
            return;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            HaRoom room = rooms[i];
            if (room.NodeIndices == null || room.NodeIndices.Count == 0)
            {
                continue;
            }

            Color color = GetRoomColor(room.Id);
            Gizmos.color = color;
            Vector3 size = Vector3.one * gizmoNodeSize;

            for (int j = 0; j < room.NodeIndices.Count; j++)
            {
                Vector3 position = nodes[room.NodeIndices[j]].WorldPosition;
                Gizmos.DrawCube(position, size);
            }
        }
    }

    private void DrawPortalGizmos()
    {
        if (portals == null)
        {
            return;
        }

        for (int i = 0; i < portals.Count; i++)
        {
            HaPortal portal = portals[i];
            if (portal.FromNodeIndex < 0 || portal.FromNodeIndex >= nodes.Length
                || portal.ToNodeIndex < 0 || portal.ToNodeIndex >= nodes.Length)
            {
                continue;
            }

            Gizmos.color = portal.Type == HaPortalType.SpecialLink ? specialLinkGizmoColor : portalGizmoColor;
            Vector3 from = nodes[portal.FromNodeIndex].WorldPosition;
            Vector3 to = nodes[portal.ToNodeIndex].WorldPosition;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawSphere(from, gizmoNodeSize * 0.5f);
            Gizmos.DrawSphere(to, gizmoNodeSize * 0.5f);
        }
    }

    private void DrawSpecialTileGizmos()
    {
        Gizmos.color = specialTileGizmoColor;
        Vector3 size = Vector3.one * gizmoNodeSize;

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].Interaction == null)
            {
                continue;
            }

            Gizmos.DrawCube(nodes[i].WorldPosition, size);
        }
    }

    public void RebuildAbstractGraph(bool rebuildGrid = false)
    {
        if (rebuildGrid || nodes == null || nodes.Length == 0)
        {
            CreateGrid(false);
            return;
        }

        BuildRoomsAndPortals(false);
    }

    public static void NotifyObstacleChanged(Vector2 worldPosition, bool rebuildAbstractGraph = true)
    {
        for (int i = 0; i < ActivePathfinders.Count; i++)
        {
            ActivePathfinders[i].RefreshNodeAtWorldPosition(worldPosition, rebuildAbstractGraph);
        }
    }

    public void RefreshNodeAtWorldPosition(Vector2 worldPosition, bool rebuildAbstractGraph = true)
    {
        if (nodes == null || nodes.Length == 0)
        {
            CreateGrid(false);
        }

        int index = NodeIndexFromWorldPoint(worldPosition);
        if (index < 0 || index >= nodes.Length)
        {
            return;
        }

        UpdateNodeAtIndex(index);
        lowLevelCostCache.Clear();
        roomLowLevelCostCache.Clear();

        if (rebuildAbstractGraph)
        {
            BuildRoomsAndPortals(false);
        }
    }

    private void CreateGrid()
    {
        CreateGrid(true);
    }

    private void CreateGrid(bool allowCachedAbstractData)
    {
        ResolveGridBounds();
        EnsureGridSettings();

        if (allowCachedAbstractData && useCachedGridData && TryLoadCachedGridData())
        {
            if (TryLoadCachedAbstractData())
            {
                return;
            }

            BuildRoomsAndPortals(false);
            return;
        }

        BuildGrid();

        if (allowCachedAbstractData && TryLoadCachedAbstractData())
        {
            return;
        }

        BuildRoomsAndPortals(false);
    }

    private void BuildGrid()
    {
        ResolveGridBounds();
        EnsureGridSettings();
        nodes = new HaNode[gridSizeX * gridSizeY];
        Vector2 worldBottomLeft = gridWorldBottomLeft;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector2 worldPoint = worldBottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius)
                                                   + Vector2.up * (y * nodeDiameter + nodeRadius);
                bool walkable = TryGetWalkable(worldPoint, out ISpecialTile interaction);

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
                    RoomId = -1,
                    Flags = walkable ? HaNodeFlags.Walkable : HaNodeFlags.None,
                    Interaction = interaction
                };
            }
        }

        BuildNeighbors();
        EnsureSearchArrays();
    }

    private bool TryLoadCachedGridData()
    {
        if (cachedWalkable == null || cachedWalkable.Length == 0)
        {
            return false;
        }

        if (cachedGridSizeX != gridSizeX || cachedGridSizeY != gridSizeY)
        {
            return false;
        }

        if (!IsCachedGridCompatible())
        {
            return false;
        }

        nodes = new HaNode[gridSizeX * gridSizeY];
        Vector2 worldBottomLeft = gridWorldBottomLeft;
        Dictionary<int, ISpecialTile> cachedSpecialTileMap = BuildCachedSpecialTileMap();

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                int index = GetIndex(x, y);
                Vector2 worldPoint = worldBottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius)
                                                   + Vector2.up * (y * nodeDiameter + nodeRadius);
                cachedSpecialTileMap.TryGetValue(index, out ISpecialTile interaction);
                bool walkable = cachedWalkable[index];
                if (interaction != null)
                {
                    walkable = true;
                }

                nodes[index] = new HaNode
                {
                    GridPosition = new Vector2Int(x, y),
                    WorldPosition = worldPoint,
                    Walkable = walkable,
                    GCost = 0,
                    HCost = 0,
                    ParentIndex = -1,
                    Neighbors = Array.Empty<int>(),
                    RoomId = -1,
                    Flags = walkable ? HaNodeFlags.Walkable : HaNodeFlags.None,
                    Interaction = interaction
                };
            }
        }

        BuildNeighbors();
        EnsureSearchArrays();
        return true;
    }

    private bool IsCachedGridCompatible()
    {
        if (!Mathf.Approximately(cachedNodeRadius, nodeRadius))
        {
            return false;
        }

        float sizeDelta = (cachedGridWorldSize - gridWorldSize).sqrMagnitude;
        if (sizeDelta > 0.0001f)
        {
            return false;
        }

        float bottomLeftDelta = (cachedGridWorldBottomLeft - gridWorldBottomLeft).sqrMagnitude;
        return bottomLeftDelta <= 0.0001f;
    }

    private Dictionary<int, ISpecialTile> BuildCachedSpecialTileMap()
    {
        Dictionary<int, ISpecialTile> map = new();
        if (cachedSpecialTiles == null)
        {
            return map;
        }

        for (int i = 0; i < cachedSpecialTiles.Count; i++)
        {
            CachedSpecialTileEntry entry = cachedSpecialTiles[i];
            if (entry.Tile == null)
            {
                continue;
            }

            if (entry.Tile is ISpecialTile specialTile)
            {
                map[entry.NodeIndex] = specialTile;
            }
        }

        return map;
    }

    private void ResolveGridBounds()
    {
        if (autoSizeGridToTilemap && floorTilemap != null)
        {
            BoundsInt bounds = floorTilemap.cellBounds;
            Vector3 cellSize = floorTilemap.cellSize;
            Vector2 size = new(bounds.size.x * cellSize.x, bounds.size.y * cellSize.y);
            size += gridWorldPadding;
            gridWorldSize = size;
            Vector3 worldMin = floorTilemap.CellToWorld(bounds.min);
            gridWorldBottomLeft = new Vector2(worldMin.x, worldMin.y) - gridWorldPadding * 0.5f;
            return;
        }

        gridWorldBottomLeft = (Vector2)transform.position - Vector2.right * gridWorldSize.x / 2f - Vector2.up * gridWorldSize.y / 2f;
    }

    private void EnsureGridSettings()
    {
        nodeDiameter = Mathf.Max(0.01f, nodeRadius * 2f);
        gridSizeX = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.x / nodeDiameter));
        gridSizeY = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.y / nodeDiameter));
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

    private void BuildRoomsAndPortals(bool cacheResults)
    {
        rooms.Clear();
        portals.Clear();
        abstractNodes.Clear();
        lowLevelCostCache.Clear();
        roomPortalCostCache.Clear();
        roomLowLevelCostCache.Clear();
        roomAbstractNodes = null;
        abstractNodeToGridIndex = null;
        portalAbstractA = null;
        portalAbstractB = null;

        AssignRoomsFromFloor();
        BuildRoomBoundaryPortals();
        BuildSpecialLinkPortals();
        BuildRoomPortalCosts();
        BuildAbstractNeighbors();

        if (cacheResults)
        {
            CacheGridData();
            CacheAbstractData();
        }
    }

    private bool TryLoadCachedAbstractData()
    {
        if (!useCachedAbstractData || nodes == null || nodes.Length == 0)
        {
            return false;
        }

        if (cachedRoomIds == null || cachedRoomIds.Length != nodes.Length)
        {
            return false;
        }

        if (cachedGridSizeX != gridSizeX || cachedGridSizeY != gridSizeY)
        {
            return false;
        }

        if (rooms == null || rooms.Count == 0)
        {
            return false;
        }

        ApplyCachedRoomIds();
        RestoreAbstractNodeToGridIndex();
        RestorePortalMappings();
        RestoreRoomAbstractNodes();
        EnsureAbstractNeighbors();
        RestoreRoomPortalCosts();
        lowLevelCostCache.Clear();
        roomLowLevelCostCache.Clear();
        return true;
    }

    private void CacheAbstractData()
    {
        cachedGridSizeX = gridSizeX;
        cachedGridSizeY = gridSizeY;

        if (nodes != null)
        {
            if (cachedRoomIds == null || cachedRoomIds.Length != nodes.Length)
            {
                cachedRoomIds = new int[nodes.Length];
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                cachedRoomIds[i] = nodes[i].RoomId;
            }
        }

        CacheRoomAbstractNodes();
        CacheRoomPortalCosts();
    }

    private void CacheGridData()
    {
        cachedGridSizeX = gridSizeX;
        cachedGridSizeY = gridSizeY;
        cachedGridWorldSize = gridWorldSize;
        cachedGridWorldBottomLeft = gridWorldBottomLeft;
        cachedNodeRadius = nodeRadius;

        if (nodes == null)
        {
            return;
        }

        if (cachedWalkable == null || cachedWalkable.Length != nodes.Length)
        {
            cachedWalkable = new bool[nodes.Length];
        }

        if (cachedSpecialTiles == null)
        {
            cachedSpecialTiles = new List<CachedSpecialTileEntry>();
        }

        cachedSpecialTiles.Clear();

        for (int i = 0; i < nodes.Length; i++)
        {
            cachedWalkable[i] = nodes[i].Walkable;
            if (nodes[i].Interaction is MonoBehaviour interactionComponent)
            {
                cachedSpecialTiles.Add(new CachedSpecialTileEntry
                {
                    NodeIndex = i,
                    Tile = interactionComponent
                });
            }
        }
    }

    private void ApplyCachedRoomIds()
    {
        for (int i = 0; i < cachedRoomIds.Length; i++)
        {
            HaNode node = nodes[i];
            node.RoomId = cachedRoomIds[i];
            nodes[i] = node;
        }
    }

    private void RestoreAbstractNodeToGridIndex()
    {
        if (abstractNodes == null)
        {
            return;
        }

        if (abstractNodeToGridIndex != null && abstractNodeToGridIndex.Length == abstractNodes.Count)
        {
            return;
        }

        abstractNodeToGridIndex = new int[abstractNodes.Count];
        for (int i = 0; i < abstractNodes.Count; i++)
        {
            HaAbstractNode node = abstractNodes[i];
            if (node.PortalIndex < 0 || node.PortalIndex >= portals.Count)
            {
                abstractNodeToGridIndex[i] = -1;
                continue;
            }

            HaPortal portal = portals[node.PortalIndex];
            abstractNodeToGridIndex[i] = portal.RoomA == node.RoomId ? portal.FromNodeIndex : portal.ToNodeIndex;
        }
    }

    private void RestorePortalMappings()
    {
        if (portals == null)
        {
            return;
        }

        if (portalAbstractA != null && portalAbstractB != null
            && portalAbstractA.Length == portals.Count
            && portalAbstractB.Length == portals.Count)
        {
            return;
        }

        portalAbstractA = new int[portals.Count];
        portalAbstractB = new int[portals.Count];
        Array.Fill(portalAbstractA, -1);
        Array.Fill(portalAbstractB, -1);

        for (int i = 0; i < abstractNodes.Count; i++)
        {
            int portalIndex = abstractNodes[i].PortalIndex;
            if (portalIndex < 0 || portalIndex >= portals.Count)
            {
                continue;
            }

            if (portalAbstractA[portalIndex] < 0)
            {
                portalAbstractA[portalIndex] = i;
            }
            else
            {
                portalAbstractB[portalIndex] = i;
            }
        }
    }

    private void RestoreRoomAbstractNodes()
    {
        roomAbstractNodes = new List<int>[rooms.Count];
        for (int i = 0; i < rooms.Count; i++)
        {
            roomAbstractNodes[i] = new List<int>();
        }

        if (cachedRoomAbstractNodes != null && cachedRoomAbstractNodes.Count == rooms.Count)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                RoomAbstractNodeList cached = cachedRoomAbstractNodes[i];
                if (cached?.Nodes == null)
                {
                    continue;
                }

                roomAbstractNodes[i].AddRange(cached.Nodes);
            }

            return;
        }

        if (abstractNodes == null)
        {
            return;
        }

        for (int i = 0; i < abstractNodes.Count; i++)
        {
            int roomId = abstractNodes[i].RoomId;
            if (roomId >= 0 && roomId < roomAbstractNodes.Length)
            {
                roomAbstractNodes[roomId].Add(i);
            }
        }
    }

    private void RestoreRoomPortalCosts()
    {
        roomPortalCostCache.Clear();
        if (cachedRoomPortalCosts != null && cachedRoomPortalCosts.Count > 0)
        {
            foreach (PortalCostEntry entry in cachedRoomPortalCosts)
            {
                long key = GetPortalCostKey(entry.AbstractIndexA, entry.AbstractIndexB);
                roomPortalCostCache[key] = entry.Cost;
            }

            return;
        }

        if (roomAbstractNodes != null && portals.Count > 0)
        {
            BuildRoomPortalCosts();
        }
    }

    private void EnsureAbstractNeighbors()
    {
        if (abstractNodes == null || abstractNodes.Count == 0)
        {
            return;
        }

        for (int i = 0; i < abstractNodes.Count; i++)
        {
            if (abstractNodes[i].Neighbors == null || abstractNodes[i].Neighbors.Length == 0)
            {
                BuildAbstractNeighbors();
                return;
            }
        }
    }

    private void CacheRoomAbstractNodes()
    {
        if (cachedRoomAbstractNodes == null)
        {
            cachedRoomAbstractNodes = new List<RoomAbstractNodeList>();
        }

        cachedRoomAbstractNodes.Clear();
        if (roomAbstractNodes == null)
        {
            return;
        }

        for (int i = 0; i < roomAbstractNodes.Length; i++)
        {
            List<int> roomNodes = roomAbstractNodes[i] ?? new List<int>();
            cachedRoomAbstractNodes.Add(new RoomAbstractNodeList
            {
                Nodes = new List<int>(roomNodes)
            });
        }
    }

    private void CacheRoomPortalCosts()
    {
        if (cachedRoomPortalCosts == null)
        {
            cachedRoomPortalCosts = new List<PortalCostEntry>();
        }

        cachedRoomPortalCosts.Clear();
        foreach (KeyValuePair<long, int> entry in roomPortalCostCache)
        {
            int min = (int)(entry.Key >> 32);
            int max = (int)(entry.Key & 0xFFFFFFFF);
            cachedRoomPortalCosts.Add(new PortalCostEntry
            {
                AbstractIndexA = min,
                AbstractIndexB = max,
                Cost = entry.Value
            });
        }
    }

    private void AssignRoomsFromFloor()
    {
        bool[] visited = new bool[nodes.Length];
        int roomId = 0;

        for (int index = 0; index < nodes.Length; index++)
        {
            if (visited[index] || !IsRoomCandidate(index))
            {
                continue;
            }

            TileBase roomTile = GetRoomTile(nodes[index].WorldPosition);
            if (separateRoomsByTileType && floorTilemap != null && roomTile == null)
            {
                continue;
            }

            HaRoom room = new HaRoom(roomId);
            Queue<int> open = new Queue<int>();
            open.Enqueue(index);
            visited[index] = true;

            while (open.Count > 0)
            {
                int currentIndex = open.Dequeue();
                HaNode node = nodes[currentIndex];
                node.RoomId = roomId;
                nodes[currentIndex] = node;
                room.NodeIndices.Add(currentIndex);

                foreach (int neighbor in node.Neighbors)
                {
                    if (visited[neighbor] || !IsRoomCandidate(neighbor, roomTile))
                    {
                        continue;
                    }

                    visited[neighbor] = true;
                    open.Enqueue(neighbor);
                }
            }

            rooms.Add(room);
            roomId++;
        }
    }

    private bool IsRoomCandidate(int index)
    {
        if (!nodes[index].IsTraversable())
        {
            return false;
        }

        return IsRoomCandidate(index, GetRoomTile(nodes[index].WorldPosition));
    }

    private bool IsRoomCandidate(int index, TileBase expectedTile)
    {
        if (!nodes[index].IsTraversable())
        {
            return false;
        }

        if (!IsFloorNode(nodes[index].WorldPosition))
        {
            return false;
        }

        if (!separateRoomsByTileType || floorTilemap == null)
        {
            return true;
        }

        TileBase tile = GetRoomTile(nodes[index].WorldPosition);
        return tile != null && tile == expectedTile;
    }

    private TileBase GetRoomTile(Vector2 worldPosition)
    {
        if (floorTilemap == null)
        {
            return null;
        }

        TileBase tile = floorTilemap.GetTile(floorTilemap.WorldToCell(worldPosition));
        if (tile == null)
        {
            return null;
        }

        if (floorTiles == null || floorTiles.Count == 0)
        {
            return tile;
        }

        return floorTiles.Contains(tile) ? tile : null;
    }

    private bool IsFloorNode(Vector2 worldPosition)
    {
        return GetRoomTile(worldPosition) != null || floorTilemap == null;
    }

    private void BuildRoomBoundaryPortals()
    {
        for (int index = 0; index < nodes.Length; index++)
        {
            if (!nodes[index].IsTraversable() || nodes[index].RoomId < 0)
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

                int roomA = node.RoomId;
                int roomB = nodes[neighborIndex].RoomId;
                if (roomA < 0 || roomB < 0 || roomA == roomB)
                {
                    continue;
                }

                int portalIndex = portals.Count;
                portals.Add(new HaPortal
                {
                    FromNodeIndex = index,
                    ToNodeIndex = neighborIndex,
                    RoomA = roomA,
                    RoomB = roomB,
                    Cost = GetDistance(index, neighborIndex),
                    Type = HaPortalType.Border
                });

                rooms[roomA].PortalIndices.Add(portalIndex);
                rooms[roomB].PortalIndices.Add(portalIndex);

                int abstractIndexA = abstractNodes.Count;
                abstractNodes.Add(new HaAbstractNode
                {
                    PortalIndex = portalIndex,
                    RoomId = roomA,
                    GCost = 0,
                    HCost = 0,
                    ParentIndex = -1,
                    Neighbors = Array.Empty<int>()
                });

                int abstractIndexB = abstractNodes.Count;
                abstractNodes.Add(new HaAbstractNode
                {
                    PortalIndex = portalIndex,
                    RoomId = roomB,
                    GCost = 0,
                    HCost = 0,
                    ParentIndex = -1,
                    Neighbors = Array.Empty<int>()
                });

                AddAbstractMapping(abstractIndexA, index, roomA, portalIndex, abstractIndexB, neighborIndex, roomB);
            }
        }
    }

    private void BuildSpecialLinkPortals()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        HashSet<(EntityId, EntityId)> processedLinks = new();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is not IHaSpecialLink link)
            {
                continue;
            }

            Transform linkTransform = link.LinkTransform;
            Transform otherTransform = link.OtherLinkTransform;
            if (linkTransform == null || otherTransform == null)
            {
                continue;
            }

            EntityId idA = linkTransform.GetEntityId();
            EntityId idB = otherTransform.GetEntityId();

            (EntityId, EntityId) key = idA <= idB ? (idA, idB) : (idB, idA);

            if (!processedLinks.Add(key))
            {
                continue;
            }

            int nodeA = NodeIndexFromWorldPoint(linkTransform.position);
            int nodeB = NodeIndexFromWorldPoint(otherTransform.position);
            if (nodeA < 0 || nodeB < 0)
            {
                continue;
            }

            int roomA = nodes[nodeA].RoomId;
            int roomB = nodes[nodeB].RoomId;
            if (roomA < 0 || roomB < 0 || roomA == roomB)
            {
                continue;
            }

            int portalIndex = portals.Count;
            int traversalCost = link.TraversalCost > 0 ? link.TraversalCost : GetDistance(nodeA, nodeB);
            portals.Add(new HaPortal
            {
                FromNodeIndex = nodeA,
                ToNodeIndex = nodeB,
                RoomA = roomA,
                RoomB = roomB,
                Cost = traversalCost,
                Type = HaPortalType.SpecialLink
            });

            rooms[roomA].PortalIndices.Add(portalIndex);
            rooms[roomB].PortalIndices.Add(portalIndex);

            int abstractIndexA = abstractNodes.Count;
            abstractNodes.Add(new HaAbstractNode
            {
                PortalIndex = portalIndex,
                RoomId = roomA,
                GCost = 0,
                HCost = 0,
                ParentIndex = -1,
                Neighbors = Array.Empty<int>()
            });

            int abstractIndexB = abstractNodes.Count;
            abstractNodes.Add(new HaAbstractNode
            {
                PortalIndex = portalIndex,
                RoomId = roomB,
                GCost = 0,
                HCost = 0,
                ParentIndex = -1,
                Neighbors = Array.Empty<int>()
            });

            AddAbstractMapping(abstractIndexA, nodeA, roomA, portalIndex, abstractIndexB, nodeB, roomB);
        }
    }

    private void BuildRoomPortalCosts()
    {
        if (roomAbstractNodes == null)
        {
            return;
        }

        for (int roomId = 0; roomId < roomAbstractNodes.Length; roomId++)
        {
            List<int> roomNodes = roomAbstractNodes[roomId];
            if (roomNodes == null || roomNodes.Count < 2)
            {
                continue;
            }

            for (int i = 0; i < roomNodes.Count; i++)
            {
                int abstractIndexA = roomNodes[i];
                int nodeA = abstractNodeToGridIndex[abstractIndexA];
                for (int j = i + 1; j < roomNodes.Count; j++)
                {
                    int abstractIndexB = roomNodes[j];
                    int nodeB = abstractNodeToGridIndex[abstractIndexB];
                    int cost = FindLowLevelCostInRoom(nodeA, nodeB, roomId);
                    if (cost == int.MaxValue)
                    {
                        continue;
                    }

                    long key = GetPortalCostKey(abstractIndexA, abstractIndexB);
                    roomPortalCostCache[key] = cost;
                }
            }
        }
    }

    private long GetPortalCostKey(int abstractIndexA, int abstractIndexB)
    {
        int min = Mathf.Min(abstractIndexA, abstractIndexB);
        int max = Mathf.Max(abstractIndexA, abstractIndexB);
        return ((long)min << 32) | (uint)max;
    }

    private int GetRoomPortalCost(int abstractIndexA, int abstractIndexB)
    {
        long key = GetPortalCostKey(abstractIndexA, abstractIndexB);
        return roomPortalCostCache.TryGetValue(key, out int cost) ? cost : int.MaxValue;
    }

    private int GetCachedRoomCost(int startIndex, int endIndex, int roomId)
    {
        if (startIndex == endIndex)
        {
            return 0;
        }

        int min = Mathf.Min(startIndex, endIndex);
        int max = Mathf.Max(startIndex, endIndex);
        var key = (roomId, min, max);
        if (roomLowLevelCostCache.TryGetValue(key, out int cachedCost))
        {
            return cachedCost;
        }

        int cost = FindLowLevelCostInRoom(startIndex, endIndex, roomId);
        roomLowLevelCostCache[key] = cost;
        return cost;
    }

    private void AddAbstractMapping(int abstractIndexA, int gridIndexA, int roomA, int portalIndex, int abstractIndexB, int gridIndexB, int roomB)
    {
        if (abstractNodeToGridIndex == null || abstractNodeToGridIndex.Length < abstractNodes.Count)
        {
            Array.Resize(ref abstractNodeToGridIndex, abstractNodes.Count);
        }

        if (roomAbstractNodes == null || roomAbstractNodes.Length != rooms.Count)
        {
            roomAbstractNodes = new List<int>[rooms.Count];
            for (int i = 0; i < rooms.Count; i++)
            {
                roomAbstractNodes[i] = new List<int>();
            }
        }

        if (portalAbstractA == null || portalAbstractA.Length < portals.Count)
        {
            Array.Resize(ref portalAbstractA, portals.Count);
            Array.Resize(ref portalAbstractB, portals.Count);
        }

        abstractNodeToGridIndex[abstractIndexA] = gridIndexA;
        abstractNodeToGridIndex[abstractIndexB] = gridIndexB;
        roomAbstractNodes[roomA].Add(abstractIndexA);
        roomAbstractNodes[roomB].Add(abstractIndexB);
        portalAbstractA[portalIndex] = abstractIndexA;
        portalAbstractB[portalIndex] = abstractIndexB;
    }

    private void BuildAbstractNeighbors()
    {
        if (roomAbstractNodes == null)
        {
            return;
        }

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

            List<int> roomNodes = roomAbstractNodes[node.RoomId];
            for (int j = 0; j < roomNodes.Count; j++)
            {
                int candidate = roomNodes[j];
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
        Vector2 worldBottomLeft = gridWorldBottomLeft;
        float percentX = Mathf.Clamp01((worldPosition.x - worldBottomLeft.x) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPosition.y - worldBottomLeft.y) / gridWorldSize.y);

        int x = Mathf.Clamp(Mathf.RoundToInt((gridSizeX - 1) * percentX), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((gridSizeY - 1) * percentY), 0, gridSizeY - 1);

        return GetIndex(x, y);
    }

    private int GetIndex(int x, int y) => x + y * gridSizeX;

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

    private int FindLowLevelCostInRoom(int startIndex, int endIndex, int roomId)
    {
        if (roomId < 0 || nodes[startIndex].RoomId != roomId || nodes[endIndex].RoomId != roomId)
        {
            return int.MaxValue;
        }

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

                if (!nodes[neighborIndex].IsTraversable() || nodes[neighborIndex].RoomId != roomId)
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
        int startRoom = nodes[startIndex].RoomId;
        int endRoom = nodes[endIndex].RoomId;
        if (abstractNodes.Count == 0 || roomAbstractNodes == null)
        {
            return new List<int>();
        }
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

            EnumerateAbstractNeighbors(currentIndex, startIndex, endIndex, startRoom, endRoom);
            foreach (int neighborIndex in abstractNeighborBuffer)
            {
                if (abstractClosedFlags[neighborIndex])
                {
                    continue;
                }

                int cost = GetAbstractTransitionCost(currentIndex, neighborIndex, startIndex, endIndex, startRoom, endRoom, virtualStart, virtualEnd);
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

    private void EnumerateAbstractNeighbors(int nodeIndex, int startIndex, int endIndex, int startRoom, int endRoom)
    {
        abstractNeighborBuffer.Clear();
        int virtualStart = abstractNodes.Count;
        int virtualEnd = abstractNodes.Count + 1;

        if (nodeIndex == virtualStart)
        {
            abstractNeighborBuffer.AddRange(roomAbstractNodes[startRoom]);
            return;
        }

        if (nodeIndex == virtualEnd)
        {
            return;
        }

        HaAbstractNode node = abstractNodes[nodeIndex];
        abstractNeighborBuffer.AddRange(node.Neighbors);
        if (node.RoomId == endRoom)
        {
            abstractNeighborBuffer.Add(virtualEnd);
        }
    }

    private int GetAbstractTransitionCost(int fromIndex, int toIndex, int startIndex, int endIndex, int startRoom, int endRoom, int virtualStart, int virtualEnd)
    {
        if (fromIndex == virtualStart)
        {
            return GetCachedRoomCost(startIndex, GetAbstractNodeGridIndex(toIndex, startIndex, endIndex), startRoom);
        }

        if (toIndex == virtualEnd)
        {
            return GetCachedRoomCost(GetAbstractNodeGridIndex(fromIndex, startIndex, endIndex), endIndex, endRoom);
        }

        if (fromIndex >= abstractNodes.Count || toIndex >= abstractNodes.Count)
        {
            return int.MaxValue;
        }

        HaAbstractNode fromNode = abstractNodes[fromIndex];
        HaAbstractNode toNode = abstractNodes[toIndex];
        if (fromNode.PortalIndex == toNode.PortalIndex && fromNode.RoomId != toNode.RoomId)
        {
            return portals[fromNode.PortalIndex].Cost;
        }

        if (fromNode.RoomId != toNode.RoomId)
        {
            return int.MaxValue;
        }

        return GetRoomPortalCost(fromIndex, toIndex);
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

    private List<int> BuildFinalPathFromAbstract(int startIndex, int endIndex, List<int> abstractPath)
    {
        List<int> finalPath = new();
        if (abstractPath.Count == 0)
        {
            return finalPath;
        }

        int virtualStart = abstractNodes.Count;
        int virtualEnd = abstractNodes.Count + 1;
        int currentNodeIndex = startIndex;
        int currentRoom = nodes[startIndex].RoomId;

        for (int i = 1; i < abstractPath.Count; i++)
        {
            int fromAbstract = abstractPath[i - 1];
            int toAbstract = abstractPath[i];

            if (toAbstract == virtualStart)
            {
                continue;
            }

            if (fromAbstract == virtualStart)
            {
                int targetNode = GetAbstractNodeGridIndex(toAbstract, startIndex, endIndex);
                if (!TryAppendRoomPath(finalPath, currentNodeIndex, targetNode, currentRoom))
                {
                    return new List<int>();
                }

                currentNodeIndex = targetNode;
                currentRoom = nodes[currentNodeIndex].RoomId;
                continue;
            }

            if (toAbstract == virtualEnd)
            {
                if (!TryAppendRoomPath(finalPath, currentNodeIndex, endIndex, currentRoom))
                {
                    return new List<int>();
                }

                currentNodeIndex = endIndex;
                break;
            }

            HaAbstractNode fromNode = abstractNodes[fromAbstract];
            HaAbstractNode toNode = abstractNodes[toAbstract];
            int fromGrid = abstractNodeToGridIndex[fromAbstract];
            int toGrid = abstractNodeToGridIndex[toAbstract];

            if (fromNode.RoomId == toNode.RoomId)
            {
                if (!TryAppendRoomPath(finalPath, currentNodeIndex, toGrid, fromNode.RoomId))
                {
                    return new List<int>();
                }

                currentNodeIndex = toGrid;
                currentRoom = fromNode.RoomId;
                continue;
            }

            if (currentNodeIndex != fromGrid)
            {
                if (!TryAppendRoomPath(finalPath, currentNodeIndex, fromGrid, fromNode.RoomId))
                {
                    return new List<int>();
                }
            }

            HaPortal portal = portals[fromNode.PortalIndex];
            if (portal.Type == HaPortalType.SpecialLink && fromNode.PortalIndex == toNode.PortalIndex)
            {
                if (finalPath.Count == 0 || finalPath[finalPath.Count - 1] != toGrid)
                {
                    finalPath.Add(toGrid);
                }

                currentNodeIndex = toGrid;
                currentRoom = toNode.RoomId;
                continue;
            }

            if (finalPath.Count == 0 || finalPath[finalPath.Count - 1] != toGrid)
            {
                finalPath.Add(toGrid);
            }

            currentNodeIndex = toGrid;
            currentRoom = toNode.RoomId;
        }

        return finalPath;
    }

    private bool TryAppendRoomPath(List<int> finalPath, int startIndex, int endIndex, int roomId)
    {
        List<int> segment = FindLowLevelPathIndicesInRoom(startIndex, endIndex, roomId);
        if (segment.Count == 0)
        {
            return false;
        }

        if (finalPath.Count > 0)
        {
            segment.RemoveAt(0);
        }

        finalPath.AddRange(segment);
        return true;
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

    private static Color GetRoomColor(int roomId)
    {
        float hue = Mathf.Repeat(roomId * 0.1618f, 1f);
        return Color.HSVToRGB(hue, 0.6f, 1f);
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

    private LayerMask GetObstacleMask()
    {
        return Obstacles | playerCollisionMask;
    }

    private bool TryGetSpecialTile(Vector2 worldPoint, out ISpecialTile interaction)
    {
        interaction = null;
        if (specialTileMask == 0)
        {
            return false;
        }

        Collider2D specialCollider = Physics2D.OverlapCircle(worldPoint, nodeRadius, specialTileMask);
        if (specialCollider == null)
        {
            return false;
        }

        interaction = specialCollider.GetComponentInParent<ISpecialTile>() ?? specialCollider.GetComponent<ISpecialTile>();
        return interaction != null;
    }

    private void UpdateNodeAtIndex(int index)
    {
        if (nodes == null || index < 0 || index >= nodes.Length)
        {
            return;
        }

        HaNode node = nodes[index];
        Vector2 worldPoint = node.WorldPosition;
        bool walkable = TryGetWalkable(worldPoint, out ISpecialTile interaction);

        node.Walkable = walkable;
        node.Interaction = interaction;
        nodes[index] = node;
    }

    private float GetCollisionRadius()
    {
        return Mathf.Max(0.001f, nodeRadius - Mathf.Max(0f, collisionRadiusShrink));
    }

    private bool TryGetWalkable(Vector2 worldPoint, out ISpecialTile interaction)
    {
        TryGetSpecialTile(worldPoint, out interaction);
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPoint, GetCollisionRadius(), GetObstacleMask());
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            if (IsSpecialTileCollider(hit))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool IsSpecialTileCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        return collider.GetComponentInParent<ISpecialTile>() != null
            || collider.GetComponent<ISpecialTile>() != null
            || collider.GetComponentInParent<IHaSpecialLink>() != null
            || collider.GetComponent<IHaSpecialLink>() != null;
    }

    [Serializable]
    private sealed class RoomAbstractNodeList
    {
        public List<int> Nodes = new();
    }

    [Serializable]
    private struct CachedSpecialTileEntry
    {
        public int NodeIndex;
        public MonoBehaviour Tile;
    }

    [Serializable]
    private struct PortalCostEntry
    {
        public int AbstractIndexA;
        public int AbstractIndexB;
        public int Cost;
    }
}
