using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum HaNodeFlags : byte
{
    None = 0,
    Walkable = 1 << 0,
    Dirty = 1 << 1
}

public struct HaNode
{
    public Vector2Int GridPosition;
    public Vector2 WorldPosition;
    public int GCost;
    public int HCost;
    public int ParentIndex;
    public int[] Neighbors;
    public int ClusterId;
    public HaNodeFlags Flags;
    public ISpecialTile Interaction;

    public int FCost => GCost + HCost;

    public bool Walkable
    {
        readonly get => (Flags & HaNodeFlags.Walkable) == HaNodeFlags.Walkable;
        set
        {
            if (value)
            {
                Flags |= HaNodeFlags.Walkable;
            }
            else
            {
                Flags &= ~HaNodeFlags.Walkable;
            }
        }
    }

    public readonly bool IsTraversable()
    {
        if (!Walkable) return false;
        if (Interaction != null)
        {
            return Interaction.CanPass();
        }

        return true;
    }
}

public struct HaPortal
{
    public int FromNodeIndex;
    public int ToNodeIndex;
    public int Cost;
}

public struct HaAbstractNode
{
    public int PortalIndex;
    public int ClusterId;
    public int GCost;
    public int HCost;
    public int ParentIndex;
    public int[] Neighbors;

    public int FCost => GCost + HCost;
}

public sealed class HaCluster
{
    public int Id { get; }
    public List<int> NodeIndices { get; }
    public List<int> PortalIndices { get; }

    public HaCluster(int id, int initialNodeCapacity = 0, int initialPortalCapacity = 0)
    {
        Id = id;
        NodeIndices = initialNodeCapacity > 0 ? new List<int>(initialNodeCapacity) : new List<int>();
        PortalIndices = initialPortalCapacity > 0 ? new List<int>(initialPortalCapacity) : new List<int>();
    }
}
