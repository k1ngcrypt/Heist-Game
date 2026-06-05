using System;
using System.Collections.Generic;
using HeistGame.Door;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinder : MonoBehaviour
{
    // Separate obstacle and player masks so path rules can change without changing scene colliders.
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
    private LayerMask specialTileMask;

    // Rooms are derived from floor tiles so the graph matches authored level layout.
    [Header("Room Settings")]
    [SerializeField]
    private Tilemap floorTilemap;

    [SerializeField]
    private List<TileBase> floorTiles = new();

    [SerializeField]
    private bool separateRoomsByTileType = true;

    // Cached data keeps editor rebuilds fast when the grid configuration has not changed.
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
    [SerializeField, HideInInspector]
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
    private PortalAbstractPair[] portalAbstractPairs;
    private TileBase[] roomTileCache;
    private readonly Dictionary<long, int> lowLevelCostCache = new();
    private readonly Dictionary<long, int> roomPortalCostCache = new();
    private readonly Dictionary<(int roomId, int min, int max), int> roomLowLevelCostCache = new();

    private readonly SearchState lowLevelSearch = new();
    private readonly SearchState abstractSearch = new();
    private readonly MinHeap lowLevelOpenSet = new();
    private readonly MinHeap abstractOpenSet = new();
    private readonly List<int> abstractNeighborBuffer = new();
    private readonly List<int> neighborIndexBuffer = new();
    private readonly List<int> pathIndexBuffer = new();
    private readonly Queue<int> roomSearchQueue = new();
    private readonly List<IHaSpecialLink> specialLinkBuffer = new();
    private static readonly List<Pathfinder> ActivePathfinders = new();
    private static readonly HashSet<IHaSpecialLink> ActiveSpecialLinks = new();

    // Build once on load so runtime path requests do not pay setup cost.
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

        // Resolve path endpoints to node indices first so the search works on a stable grid.
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

        // Use the local grid path when both points are in one room; the abstract graph is only needed across rooms.
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

        if (TryFindLowLevelPath(startIndex, endIndex, roomId, true, true, out int _, out List<int> path))
        {
            return path;
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

    public static void RegisterSpecialLink(IHaSpecialLink link)
    {
        if (link == null)
        {
            return;
        }

        if (!ActiveSpecialLinks.Add(link))
        {
            return;
        }

        if (!Application.isPlaying)
        {
            return;
        }

        for (int i = 0; i < ActivePathfinders.Count; i++)
        {
            ActivePathfinders[i].RebuildAbstractGraph(false);
        }
    }

    public static void UnregisterSpecialLink(IHaSpecialLink link)
    {
        if (link == null)
        {
            return;
        }

        if (!ActiveSpecialLinks.Remove(link))
        {
            return;
        }

        if (!Application.isPlaying)
        {
            return;
        }

        for (int i = 0; i < ActivePathfinders.Count; i++)
        {
            ActivePathfinders[i].RebuildAbstractGraph(false);
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
        => CreateGrid(true);

    private void CreateGrid(bool allowCachedAbstractData)
    {
        ResolveGridBounds();
        EnsureGridSettings();

        // Reuse cached node data when the geometry is unchanged so editor and runtime rebuilds stay cheap.
        if (allowCachedAbstractData && useCachedGridData)
        {
            if (TryLoadCachedNodeData())
            {
                if (TryLoadCachedAbstractData())
                {
                    return;
                }

                BuildRoomsAndPortals(false);
                return;
            }

            if (TryLoadCachedGridData())
            {
                if (TryLoadCachedAbstractData())
                {
                    return;
                }

                BuildRoomsAndPortals(false);
                return;
            }
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

        // Each node stores enough metadata to avoid recomputing world/grid conversions during search.
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

    private bool TryLoadCachedNodeData()
    {
        if (nodes == null || nodes.Length == 0)
        {
            return false;
        }

        if (cachedGridSizeX != gridSizeX || cachedGridSizeY != gridSizeY)
        {
            return false;
        }

        if (nodes.Length != gridSizeX * gridSizeY)
        {
            return false;
        }

        if (!IsCachedGridCompatible())
        {
            return false;
        }

        ApplyCachedNodeInteractions();
        EnsureNodeNeighbors();
        EnsureSearchArrays();
        return true;
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
                bool walkable = true;
                if (interaction == null)
                {
                    walkable = cachedWalkable[index];
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

    private void ApplyCachedNodeInteractions()
    {
        if (cachedSpecialTiles == null || cachedSpecialTiles.Count == 0)
        {
            return;
        }

        Dictionary<int, ISpecialTile> cachedSpecialTileMap = BuildCachedSpecialTileMap();
        if (cachedSpecialTileMap.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<int, ISpecialTile> entry in cachedSpecialTileMap)
        {
            if (entry.Key < 0 || entry.Key >= nodes.Length)
            {
                continue;
            }

            HaNode node = nodes[entry.Key];
            node.Interaction = entry.Value;
            if (entry.Value != null)
            {
                node.Walkable = true;
            }

            nodes[entry.Key] = node;
        }
    }

    private void EnsureNodeNeighbors()
    {
        if (nodes == null || nodes.Length == 0)
        {
            return;
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].Neighbors == null || nodes[i].Neighbors.Length == 0)
            {
                BuildNeighbors();
                return;
            }
        }
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
        if (cachedSpecialTiles == null || cachedSpecialTiles.Count == 0)
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
        // Only cardinal links are created here so diagonal movement cannot cut across blocked corners.
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                neighborIndexBuffer.Clear();
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                        {
                            continue;
                        }

                        int checkX = x + offsetX;
                        int checkY = y + offsetY;

                        if (Mathf.Abs(offsetX) + Mathf.Abs(offsetY) == 1
                            && checkX >= 0 && checkX < gridSizeX
                            && checkY >= 0 && checkY < gridSizeY)
                        {
                            neighborIndexBuffer.Add(GetIndex(checkX, checkY));
                        }
                    }
                }

                int index = GetIndex(x, y);
                HaNode node = nodes[index];
                node.Neighbors = neighborIndexBuffer.ToArray();
                nodes[index] = node;
            }
        }
    }

    private void BuildRoomsAndPortals(bool cacheResults)
    {
        // Room/portal data forms a higher-level graph, which reduces search cost between distant rooms.
        rooms.Clear();
        portals.Clear();
        abstractNodes.Clear();
        lowLevelCostCache.Clear();
        roomPortalCostCache.Clear();
        roomLowLevelCostCache.Clear();
        roomAbstractNodes = null;
        abstractNodeToGridIndex = null;
        portalAbstractPairs = null;

        BuildRoomTileCache();
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
        AppendSpecialLinksToCachedGraph();
        EnsureAbstractNeighbors();
        RestoreRoomPortalCosts();
        
        lowLevelCostCache.Clear();
        roomLowLevelCostCache.Clear();
        return true;
    }

    private void AppendSpecialLinksToCachedGraph()
    {
        if (ActiveSpecialLinks.Count == 0)
        {
            return;
        }

        int portalCount = portals.Count;
        int abstractCount = abstractNodes.Count;

        BuildSpecialLinkPortals();

        if (portals.Count == portalCount && abstractNodes.Count == abstractCount)
        {
            return;
        }

        roomPortalCostCache.Clear();
        BuildRoomPortalCosts();
        BuildAbstractNeighbors();
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

        if (portalAbstractPairs != null && portalAbstractPairs.Length == portals.Count)
        {
            return;
        }

        portalAbstractPairs = new PortalAbstractPair[portals.Count];
        for (int i = 0; i < portalAbstractPairs.Length; i++)
        {
            portalAbstractPairs[i] = new PortalAbstractPair { A = -1, B = -1 };
        }

        for (int i = 0; i < abstractNodes.Count; i++)
        {
            int portalIndex = abstractNodes[i].PortalIndex;
            if (portalIndex < 0 || portalIndex >= portals.Count)
            {
                continue;
            }

            PortalAbstractPair pair = portalAbstractPairs[portalIndex];
            if (pair.A < 0)
            {
                pair.A = i;
            }
            else
            {
                pair.B = i;
            }

            portalAbstractPairs[portalIndex] = pair;
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
        BuildRoomTileCache();
        bool[] visited = new bool[nodes.Length];
        int roomId = 0;

        // Flood-fill contiguous walkable tiles so room grouping stays aligned with the floor art.
        for (int index = 0; index < nodes.Length; index++)
        {
            if (visited[index] || !IsRoomCandidate(index))
            {
                continue;
            }

            TileBase roomTile = GetRoomTileForIndex(index);
            if (separateRoomsByTileType && floorTilemap != null && roomTile == null)
            {
                continue;
            }

            HaRoom room = new(roomId);
            roomSearchQueue.Clear();
            roomSearchQueue.Enqueue(index);
            visited[index] = true;

            while (roomSearchQueue.Count > 0)
            {
                int currentIndex = roomSearchQueue.Dequeue();
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
                    roomSearchQueue.Enqueue(neighbor);
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

        return IsRoomCandidate(index, GetRoomTileForIndex(index));
    }

    private bool IsRoomCandidate(int index, TileBase expectedTile)
    {
        if (!nodes[index].IsTraversable())
        {
            return false;
        }

        if (floorTilemap == null)
        {
            return true;
        }

        TileBase tile = GetRoomTileForIndex(index);
        if (tile == null)
        {
            return false;
        }

        if (!separateRoomsByTileType)
        {
            return true;
        }

        return tile == expectedTile;
    }

    private TileBase GetRoomTileForIndex(int index)
    {
        if (roomTileCache != null && index >= 0 && index < roomTileCache.Length)
        {
            return roomTileCache[index];
        }

        return GetRoomTile(nodes[index].WorldPosition);
    }

    private void BuildRoomTileCache()
    {
        if (floorTilemap == null || nodes == null || nodes.Length == 0)
        {
            roomTileCache = null;
            return;
        }

        if (roomTileCache == null || roomTileCache.Length != nodes.Length)
        {
            roomTileCache = new TileBase[nodes.Length];
        }

        HashSet<TileBase> allowedTiles = null;
        if (floorTiles != null && floorTiles.Count > 0)
        {
            allowedTiles = new HashSet<TileBase>(floorTiles);
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            TileBase tile = floorTilemap.GetTile(floorTilemap.WorldToCell(nodes[i].WorldPosition));
            if (tile != null && allowedTiles != null && !allowedTiles.Contains(tile))
            {
                tile = null;
            }

            roomTileCache[i] = tile;
        }
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

    private void BuildRoomBoundaryPortals()
    {
        // Border portals connect adjacent rooms wherever walkable nodes touch across a room boundary.
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
        // Special links are tracked globally so linked scene objects can create portals without tight coupling.
        HashSet<(EntityId, EntityId)> processedLinks = new();
        specialLinkBuffer.Clear();

        if (!Application.isPlaying)
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IHaSpecialLink link)
                {
                    specialLinkBuffer.Add(link);
                }
            }
        }

        if (ActiveSpecialLinks.Count > 0)
        {
            specialLinkBuffer.AddRange(ActiveSpecialLinks);
        }

        for (int i = 0; i < specialLinkBuffer.Count; i++)
        {
            IHaSpecialLink link = specialLinkBuffer[i];
            if (link == null)
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
        if (roomAbstractNodes == null) return;

        for (int roomId = 0; roomId < roomAbstractNodes.Length; roomId++)
        {
            List<int> roomNodes = roomAbstractNodes[roomId];
            if (roomNodes == null || roomNodes.Count < 2) continue;

            for (int i = 0; i < roomNodes.Count; i++)
            {
                int abstractIndexA = roomNodes[i];
                int nodeA = abstractNodeToGridIndex[abstractIndexA];
                int portalIndexA = abstractNodes[abstractIndexA].PortalIndex;
                bool aIsSpecialLink = portalIndexA >= 0 && portalIndexA < portals.Count
                    && portals[portalIndexA].Type == HaPortalType.SpecialLink;

                for (int j = i + 1; j < roomNodes.Count; j++)
                {
                    int abstractIndexB = roomNodes[j];
                    int nodeB = abstractNodeToGridIndex[abstractIndexB];
                    int portalIndexB = abstractNodes[abstractIndexB].PortalIndex;
                    bool bIsSpecialLink = portalIndexB >= 0 && portalIndexB < portals.Count
                        && portals[portalIndexB].Type == HaPortalType.SpecialLink;

                    int cost = FindLowLevelCostInRoom(nodeA, nodeB, roomId);
                    if (cost == int.MaxValue) continue;

                    // A→B means the agent walks from A's grid node to B's grid node
                    // inside this room, then exits via B's portal. If B is a special
                    // link, the agent must also pay that link's traversal cost as part
                    // of committing to this inter-portal route.
                    if (bIsSpecialLink)
                        cost += portals[portalIndexB].Cost;

                    long keyAB = GetPortalCostKey(abstractIndexA, abstractIndexB);
                    roomPortalCostCache[keyAB] = cost;

                    // Store the reverse separately — A and B are asymmetric when
                    // either is a special link, so a single symmetric entry is wrong.
                    int reverseCost = FindLowLevelCostInRoom(nodeB, nodeA, roomId);
                    if (reverseCost != int.MaxValue)
                    {
                        if (aIsSpecialLink)
                            reverseCost += portals[portalIndexA].Cost;

                        // Only write the reverse key if it differs from the forward key,
                        // otherwise GetPortalCostKey's min/max ordering will collide.
                        // Use a directed key instead.
                        long keyBA = GetDirectedPortalCostKey(abstractIndexB, abstractIndexA);
                        roomPortalCostCache[keyBA] = reverseCost;
                    }
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
        long directedKey = GetDirectedPortalCostKey(abstractIndexA, abstractIndexB);
        if (roomPortalCostCache.TryGetValue(directedKey, out int directedCost))
            return directedCost;

        long symmetricKey = GetPortalCostKey(abstractIndexA, abstractIndexB);
        return roomPortalCostCache.TryGetValue(symmetricKey, out int cost) ? cost : int.MaxValue;
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

        if (portalAbstractPairs == null || portalAbstractPairs.Length < portals.Count)
        {
            int previousLength = portalAbstractPairs?.Length ?? 0;
            Array.Resize(ref portalAbstractPairs, portals.Count);
            for (int i = previousLength; i < portalAbstractPairs.Length; i++)
            {
                portalAbstractPairs[i] = new PortalAbstractPair { A = -1, B = -1 };
            }
        }

        abstractNodeToGridIndex[abstractIndexA] = gridIndexA;
        abstractNodeToGridIndex[abstractIndexB] = gridIndexB;
        roomAbstractNodes[roomA].Add(abstractIndexA);
        roomAbstractNodes[roomB].Add(abstractIndexB);
        portalAbstractPairs[portalIndex] = new PortalAbstractPair { A = abstractIndexA, B = abstractIndexB };
    }

    private void BuildAbstractNeighbors()
    {
        if (roomAbstractNodes == null) return;

        for (int i = 0; i < abstractNodes.Count; i++)
        {
            HaAbstractNode node = abstractNodes[i];
            List<int> neighbors = new();

            int portalIndex = node.PortalIndex;
            if (portalIndex >= 0 && portalIndex < portalAbstractPairs.Length) // guard added
            {
                PortalAbstractPair pair = portalAbstractPairs[portalIndex];
                int paired = pair.A == i ? pair.B : pair.A;
                //Debug.Log($"Abstract node {i}: portalIndex={portalIndex} paired={paired} roomId={node.RoomId}");
                if (paired >= 0)
                {
                    neighbors.Add(paired);
                }
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

    private int FindLowLevelCost(int startIndex, int endIndex)
    {
        if (TryFindLowLevelPath(startIndex, endIndex, -1, false, false, out int cost, out List<int> _))
        {
            return cost;
        }

        return int.MaxValue;
    }

    private int FindLowLevelCostInRoom(int startIndex, int endIndex, int roomId)
    {
        if (roomId < 0 || nodes[startIndex].RoomId != roomId || nodes[endIndex].RoomId != roomId)
        {
            return int.MaxValue;
        }
        if (TryFindLowLevelPath(startIndex, endIndex, roomId, true, false, out int cost, out List<int> _))
        {
            return cost;
        }

        return int.MaxValue;
    }

    private bool TryFindLowLevelPath(int startIndex, int endIndex, int roomId, bool constrainRoom, bool buildPath, out int cost, out List<int> path)
    {
        path = null;
        cost = int.MaxValue;

        if (startIndex == endIndex)
        {
            cost = 0;
            if (buildPath)
            {
                pathIndexBuffer.Clear();
                pathIndexBuffer.Add(startIndex);
                path = new List<int>(pathIndexBuffer);
            }

            return true;
        }

        EnsureSearchArrays();
        ResetSearchArrays();
        lowLevelOpenSet.Clear();

        lowLevelSearch.GCosts[startIndex] = 0;
        lowLevelSearch.HCosts[startIndex] = GetDistance(startIndex, endIndex);
        lowLevelSearch.Open[startIndex] = true;
        lowLevelOpenSet.EnqueueOrUpdate(startIndex);

        while (lowLevelOpenSet.Count > 0)
        {
            int currentIndex = lowLevelOpenSet.Dequeue();
            lowLevelSearch.Open[currentIndex] = false;
            lowLevelSearch.Closed[currentIndex] = true;

            if (currentIndex == endIndex)
            {
                cost = lowLevelSearch.GCosts[endIndex];
                if (buildPath)
                {
                    if (!TryBuildPathIndices(startIndex, endIndex, pathIndexBuffer))
                    {
                        return false;
                    }

                    path = new List<int>(pathIndexBuffer);
                }

                return true;
            }

            foreach (int neighborIndex in nodes[currentIndex].Neighbors)
            {
                if (lowLevelSearch.Closed[neighborIndex])
                {
                    continue;
                }

                if (!nodes[neighborIndex].IsTraversable())
                {
                    continue;
                }

                if (constrainRoom && nodes[neighborIndex].RoomId != roomId)
                {
                    continue;
                }

                int newMovementCost = lowLevelSearch.GCosts[currentIndex] + GetDistance(currentIndex, neighborIndex);
                if (newMovementCost < lowLevelSearch.GCosts[neighborIndex] || !lowLevelSearch.Open[neighborIndex])
                {
                    lowLevelSearch.GCosts[neighborIndex] = newMovementCost;
                    lowLevelSearch.HCosts[neighborIndex] = GetDistance(neighborIndex, endIndex);
                    lowLevelSearch.Parents[neighborIndex] = currentIndex;

                    if (!lowLevelSearch.Open[neighborIndex])
                    {
                        lowLevelSearch.Open[neighborIndex] = true;
                    }

                    lowLevelOpenSet.EnqueueOrUpdate(neighborIndex);
                }
            }
        }

        return false;
    }

    private bool TryBuildPathIndices(int startIndex, int endIndex, List<int> pathBuffer)
    {
        pathBuffer.Clear();
        int currentIndex = endIndex;

        while (currentIndex != startIndex)
        {
            pathBuffer.Add(currentIndex);
            currentIndex = lowLevelSearch.Parents[currentIndex];
            if (currentIndex < 0)
            {
                pathBuffer.Clear();
                return false;
            }
        }

        pathBuffer.Add(startIndex);
        pathBuffer.Reverse();
        return true;
    }

    private List<int> FindAbstractPath(int startIndex, int endIndex)
    {
        int startRoom = nodes[startIndex].RoomId;
        int endRoom = nodes[endIndex].RoomId;
        if (abstractNodes.Count == 0 || roomAbstractNodes == null)
        {
            return new List<int>();
        }
        // Virtual endpoints let the same search logic handle arbitrary start/end nodes inside their rooms.
        int totalNodes = abstractNodes.Count + 2;
        int virtualStart = abstractNodes.Count;
        int virtualEnd = abstractNodes.Count + 1;

        if (!EnsureAbstractSearchArrays(totalNodes))
        {
            return new List<int>();
        }

        ResetAbstractSearchArrays(totalNodes);
        abstractOpenSet.Clear();

        abstractSearch.GCosts[virtualStart] = 0;
        abstractSearch.HCosts[virtualStart] = GetDistance(startIndex, endIndex);
        abstractSearch.Open[virtualStart] = true;
        abstractOpenSet.EnqueueOrUpdate(virtualStart);

        while (abstractOpenSet.Count > 0)
        {
            int currentIndex = abstractOpenSet.Dequeue();
            abstractSearch.Open[currentIndex] = false;
            abstractSearch.Closed[currentIndex] = true;

            if (currentIndex == virtualEnd)
            {
                //Debug.Log($"Path found, total gCost={abstractSearch.GCosts[virtualEnd]}");
                return RetraceAbstractPath(virtualStart, virtualEnd);
            }

            EnumerateAbstractNeighbors(currentIndex, startIndex, endIndex, startRoom, endRoom);
            foreach (int neighborIndex in abstractNeighborBuffer)
            {
                if (abstractSearch.Closed[neighborIndex])
                {
                    continue;
                }

                int cost = GetAbstractTransitionCost(currentIndex, neighborIndex, startIndex, endIndex, startRoom, endRoom, virtualStart, virtualEnd);
                if (cost == int.MaxValue)
                {
                    continue;
                }

                int newMovementCost = abstractSearch.GCosts[currentIndex] + cost;
                if (newMovementCost < abstractSearch.GCosts[neighborIndex] || !abstractSearch.Open[neighborIndex])
                {
                    abstractSearch.GCosts[neighborIndex] = newMovementCost;
                    abstractSearch.HCosts[neighborIndex] = GetDistance(GetAbstractNodeGridIndex(neighborIndex, startIndex, endIndex), endIndex);
                    abstractSearch.Parents[neighborIndex] = currentIndex;

                    if (!abstractSearch.Open[neighborIndex])
                    {
                        abstractSearch.Open[neighborIndex] = true;
                    }

                    abstractOpenSet.EnqueueOrUpdate(neighborIndex);
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
            //Debug.Log($"VirtualStart: startRoom={startRoom} neighbors={string.Join(",", roomAbstractNodes[startRoom])}");
            abstractNeighborBuffer.AddRange(roomAbstractNodes[startRoom]);
            return;
        }

        if (nodeIndex == virtualEnd)
        {
            return;
        }

        HaAbstractNode node = abstractNodes[nodeIndex];
        //Debug.Log($"Expanding abstract node {nodeIndex}: roomId={node.RoomId} portalIndex={node.PortalIndex} portalType={portals[node.PortalIndex].Type} neighbors={string.Join(",", node.Neighbors)}");
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
        //Debug.Log("Cost: " + portals[fromNode.PortalIndex].Cost);
        if (fromNode.PortalIndex == toNode.PortalIndex && fromNode.RoomId != toNode.RoomId)
        {
            //int totalSoFar = abstractSearch.GCosts[fromIndex];
            //Debug.Log($"Crossing special link portal {fromNode.PortalIndex}, gCost so far={totalSoFar}, link cost={portals[fromNode.PortalIndex].Cost}, total would be={totalSoFar + portals[fromNode.PortalIndex].Cost}");
            return portals[fromNode.PortalIndex].Cost;
        }

        if (fromNode.RoomId != toNode.RoomId)
        {
            //Debug.Log($"Cross-room transition blocked: from portal {fromNode.PortalIndex} to portal {toNode.PortalIndex}, same portal = {fromNode.PortalIndex == toNode.PortalIndex}");
            return int.MaxValue;
        }

        return GetRoomPortalCost(fromIndex, toIndex);
    }

    private int GetAbstractNodeGridIndex(int abstractIndex, int startIndex, int endIndex)
        => abstractIndex switch
        {
            var index when index == abstractNodes.Count => startIndex,
            var index when index == abstractNodes.Count + 1 => endIndex,
            _ => abstractNodeToGridIndex[abstractIndex]
        };

    private List<int> RetraceAbstractPath(int startIndex, int endIndex)
    {
        List<int> path = new();
        int currentIndex = endIndex;

        while (currentIndex != startIndex)
        {
            path.Add(currentIndex);
            currentIndex = abstractSearch.Parents[currentIndex];
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
        List<Vector2> path = new(nodeIndices.Count);
        for (int i = 0; i < nodeIndices.Count; i++)
        {
            path.Add(nodes[nodeIndices[i]].WorldPosition);
        }

        return path;
    }

    private int GetDistance(int indexA, int indexB)
    {
        Vector2Int a = nodes[indexA].GridPosition;
        Vector2Int b = nodes[indexB].GridPosition;
        int dstX = Mathf.Abs(a.x - b.x);
        int dstY = Mathf.Abs(a.y - b.y);
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

        lowLevelSearch.Ensure(nodes.Length);
        lowLevelOpenSet.Initialize(lowLevelSearch.HeapIndices, lowLevelSearch.GCosts, lowLevelSearch.HCosts);
    }

    private void ResetSearchArrays()
    {
        lowLevelSearch.Reset(nodes.Length);
    }

    private bool EnsureAbstractSearchArrays(int totalNodes)
    {
        if (totalNodes <= 0)
        {
            return false;
        }

        abstractSearch.Ensure(totalNodes);
        abstractOpenSet.Initialize(abstractSearch.HeapIndices, abstractSearch.GCosts, abstractSearch.HCosts);

        return true;
    }

    private void ResetAbstractSearchArrays(int totalNodes)
    {
        abstractSearch.Reset(totalNodes);
    }

    private LayerMask GetObstacleMask()
        => Obstacles | playerCollisionMask;

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
        // Special tiles are checked first so they can override collision-based blocking when intended.
        TryGetSpecialTile(worldPoint, out interaction);
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPoint, GetCollisionRadius(), GetObstacleMask());
        if (hits.Length == 0)
        {
            return true;
        }

        foreach (Collider2D hit in hits)
        {
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

    private sealed class SearchState
    {
        public int[] GCosts;
        public int[] HCosts;
        public int[] Parents;
        public bool[] Closed;
        public bool[] Open;
        public int[] HeapIndices;

        public void Ensure(int length)
        {
            if (length <= 0)
            {
                return;
            }

            if (GCosts == null || GCosts.Length < length)
            {
                GCosts = new int[length];
                HCosts = new int[length];
                Parents = new int[length];
                Closed = new bool[length];
                Open = new bool[length];
                HeapIndices = new int[length];
            }
        }

        public void Reset(int length)
        {
            Ensure(length);
            for (int i = 0; i < length; i++)
            {
                GCosts[i] = int.MaxValue;
                HCosts[i] = 0;
                Parents[i] = -1;
                Closed[i] = false;
                Open[i] = false;
                HeapIndices[i] = -1;
            }
        }
    }

    private long GetDirectedPortalCostKey(int from, int to)
    {
        // High bit set = directed key, avoids collision with symmetric keys
        return unchecked((long)1 << 63) | ((long)(uint)from << 32) | (uint)to;
    }

    private sealed class MinHeap
    {
        private readonly List<int> heap = new();
        private int[] positions;
        private int[] gCosts;
        private int[] hCosts;

        public int Count => heap.Count;

        public void Initialize(int[] positionMap, int[] gCostMap, int[] hCostMap)
        {
            positions = positionMap;
            gCosts = gCostMap;
            hCosts = hCostMap;
        }

        public void Clear()
        {
            if (heap.Count == 0)
            {
                return;
            }

            for (int i = 0; i < heap.Count; i++)
            {
                int index = heap[i];
                if (index >= 0 && index < positions.Length)
                {
                    positions[index] = -1;
                }
            }

            heap.Clear();
        }

        public void EnqueueOrUpdate(int index)
        {
            int position = positions[index];
            if (position >= 0)
            {
                HeapifyUp(position);
                return;
            }

            positions[index] = heap.Count;
            heap.Add(index);
            HeapifyUp(heap.Count - 1);
        }

        public int Dequeue()
        {
            int result = heap[0];
            int lastIndex = heap.Count - 1;
            Swap(0, lastIndex);
            heap.RemoveAt(lastIndex);
            positions[result] = -1;

            if (heap.Count > 0)
            {
                HeapifyDown(0);
            }

            return result;
        }

        private int GetPriority(int index)
            => gCosts[index] + hCosts[index];

        private bool HasHigherPriority(int a, int b)
        {
            int costA = GetPriority(a);
            int costB = GetPriority(b);
            if (costA == costB)
            {
                return hCosts[a] < hCosts[b]; //Tiebreak by heuristic cost
            }

            return costA < costB;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (HasHigherPriority(heap[parent], heap[index]))
                {
                    break;
                }

                Swap(parent, index);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int lastIndex = heap.Count - 1;
            while (true)
            {
                int left = index * 2 + 1;
                int right = left + 1;
                if (left > lastIndex)
                {
                    return;
                }

                int best = left;
                if (right <= lastIndex && HasHigherPriority(heap[right], heap[left]))
                {
                    best = right;
                }

                if (HasHigherPriority(heap[index], heap[best]))
                {
                    return;
                }

                Swap(index, best);
                index = best;
            }
        }

        private void Swap(int a, int b)
        {
            (heap[b], heap[a]) = (heap[a], heap[b]);
            positions[heap[a]] = a;
            positions[heap[b]] = b;
        }
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
    private struct PortalAbstractPair
    {
        public int A;
        public int B;
    }

    [Serializable]
    private struct PortalCostEntry
    {
        public int AbstractIndexA;
        public int AbstractIndexB;
        public int Cost;
    }
}
