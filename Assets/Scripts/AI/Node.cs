using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum HaNodeFlags : byte
{
    None = 0,
    // Stored as a flag so future node state can be extended without changing the struct layout.
    Walkable = 1 << 0,
    Dirty = 1 << 1
}

[Serializable]
public struct HaNode
{
    // Grid and world positions are both stored to avoid repeated conversion during pathfinding.
    public Vector2Int GridPosition;
    public Vector2 WorldPosition;
    public int GCost;
    public int HCost;
    public int ParentIndex;
    public int[] Neighbors;
    public int RoomId;
    public HaNodeFlags Flags;
    [NonSerialized]
    public ISpecialTile Interaction;

    public int FCost => GCost + HCost;

    // Walkability is derived from flags so the struct can stay compact and copy-friendly.
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

    // Interaction checks stay separate from walkability so doors, vents, and other tiles can gate movement.
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

[Serializable]
public struct HaPortal
{
    public int FromNodeIndex;
    public int ToNodeIndex;
    public int RoomA;
    public int RoomB;
    public int Cost;
    public HaPortalType Type;
}

public enum HaPortalType : byte
{
    Border = 0,
    SpecialLink = 1
}

[Serializable]
public struct HaAbstractNode
{
    public int PortalIndex;
    public int RoomId;
    public int GCost;
    public int HCost;
    public int ParentIndex;
    public int[] Neighbors;

    public int FCost => GCost + HCost;
}

[Serializable]
public sealed class HaRoom
{
    public int Id;
    // Lists are used because room membership is built incrementally during flood fill.
    public List<int> NodeIndices = new();
    public List<int> PortalIndices = new();

    public HaRoom()
    {
    }

    public HaRoom(int id, int initialNodeCapacity = 0, int initialPortalCapacity = 0)
    {
        Id = id;
        NodeIndices = initialNodeCapacity > 0 ? new List<int>(initialNodeCapacity) : new List<int>();
        PortalIndices = initialPortalCapacity > 0 ? new List<int>(initialPortalCapacity) : new List<int>();
    }
}
