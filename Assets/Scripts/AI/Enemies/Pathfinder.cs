using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    [SerializeField]
    public LayerMask Obstacles;

    [SerializeField]
    private Vector2 gridWorldSize = new Vector2(20f, 20f);

    [SerializeField]
    private float nodeRadius = 0.5f;

    [SerializeField]
    private bool allowDiagonals = false;

    private float nodeDiameter;
    private int gridSizeX;
    private int gridSizeY;
    private Node[,] grid;
    private readonly List<Node> openSet = new();
    private readonly HashSet<Node> closedSet = new();

    private void Awake()
    {
        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.x / nodeDiameter));
        gridSizeY = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.y / nodeDiameter));
        CreateGrid();
    }

    public List<Node> FindPath(Vector2 startPos, Vector2 targetPos)
    {
        if (grid == null || grid.Length == 0)
        {
            CreateGrid();
        }

        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);

        if (startNode == null || targetNode == null || !startNode.Walkable || !targetNode.Walkable)
        {
            return new List<Node>();
        }

        openSet.Clear();
        closedSet.Clear();

        foreach (Node node in grid)
        {
            node.GCost = int.MaxValue;
            node.HCost = 0;
            node.Parent = null;
        }

        startNode.GCost = 0;
        startNode.HCost = GetDistance(startNode, targetNode);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(openSet);
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.Walkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int newMovementCost = currentNode.GCost + GetDistance(currentNode, neighbor);
                if (newMovementCost < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    neighbor.GCost = newMovementCost;
                    neighbor.HCost = GetDistance(neighbor, targetNode);
                    neighbor.Parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return new List<Node>();
    }

    private void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector2 worldBottomLeft = (Vector2)transform.position - Vector2.right * gridWorldSize.x / 2f - Vector2.up * gridWorldSize.y / 2f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector2 worldPoint = worldBottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius)
                                                   + Vector2.up * (y * nodeDiameter + nodeRadius);
                bool walkable = !Physics2D.OverlapCircle(worldPoint, nodeRadius, Obstacles);
                grid[x, y] = new Node(new Vector2Int(x, y), worldPoint, walkable);
            }
        }
    }

    private Node NodeFromWorldPoint(Vector2 worldPosition)
    {
        Vector2 worldBottomLeft = (Vector2)transform.position - Vector2.right * gridWorldSize.x / 2f - Vector2.up * gridWorldSize.y / 2f;
        float percentX = Mathf.Clamp01((worldPosition.x - worldBottomLeft.x) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPosition.y - worldBottomLeft.y) / gridWorldSize.y);

        int x = Mathf.Clamp(Mathf.RoundToInt((gridSizeX - 1) * percentX), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((gridSizeY - 1) * percentY), 0, gridSizeY - 1);

        return grid[x, y];
    }

    private IEnumerable<Node> GetNeighbors(Node node)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                if (!allowDiagonals && Mathf.Abs(x) + Mathf.Abs(y) > 1)
                {
                    continue;
                }

                int checkX = node.GridPosition.x + x;
                int checkY = node.GridPosition.y + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    yield return grid[checkX, checkY];
                }
            }
        }
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
            if (currentNode == null)
            {
                return new List<Node>();
            }
        }

        path.Add(startNode);
        path.Reverse();
        return path;
    }

    private Node GetLowestFCostNode(List<Node> nodes)
    {
        Node bestNode = nodes[0];
        for (int i = 1; i < nodes.Count; i++)
        {
            Node candidate = nodes[i];
            if (candidate.FCost < bestNode.FCost || (candidate.FCost == bestNode.FCost && candidate.HCost < bestNode.HCost))
            {
                bestNode = candidate;
            }
        }

        return bestNode;
    }

    private int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.GridPosition.x - b.GridPosition.x);
        int dstY = Mathf.Abs(a.GridPosition.y - b.GridPosition.y);

        if (allowDiagonals)
        {
            int remaining = Mathf.Abs(dstX - dstY);
            return 14 * Mathf.Min(dstX, dstY) + 10 * remaining;
        }

        return 10 * (dstX + dstY);
    }
}
