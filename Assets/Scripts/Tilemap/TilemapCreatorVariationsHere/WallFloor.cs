using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(Tilemap))]
public class WallFloor : DuplicateTilemapBase
{
    public RuleTile floorWallTile;
    public List<TileBase> walls;

    protected override bool OnCreation() {
        return floorWallTile != null&&Map.IsInitialized;
    }

    protected override TileBase GetTile(Vector3Int v) {
        foreach (TileBase t in walls) if (Map.IsTile(v,t)) return floorWallTile;
        return null;
    }
}
