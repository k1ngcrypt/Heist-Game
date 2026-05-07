using UnityEngine;

public class Node
{
    public Vector2Int GridPosition { get; }
    public Vector2 WorldPosition { get; }
    public bool Walkable { get; }
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    public Node Parent { get; set; }

    public Node(Vector2Int gridPosition, Vector2 worldPosition, bool walkable)
    {
        GridPosition = gridPosition;
        WorldPosition = worldPosition;
        Walkable = walkable;
        GCost = int.MaxValue;
        HCost = 0;
        Parent = null;
    }
}
