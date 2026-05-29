using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates top/corner tiles for walls based on the main tilemap layout.
/// Optimized to work with the new Map system.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class TilemapTop : DuplicateTilemapBase
{
    [Header("Tile References")]
    [SerializeField] private RuleTile WallTile;
    [SerializeField] private RuleTile TopTile, CornerTile;

    protected override bool OnCreation() {
        return WallTile != null&&TopTile != null&&CornerTile != null;
    }
    protected override TileBase GetTile(Vector3Int v)
    {
        // Check if there's a wall below and no wall at current position
        if (Map.IsTile(v.x, v.y - 1, WallTile) && !Map.IsTile(v, WallTile)) return TopTile;
        
        // Check for corner conditions
        if (Map.IsTile(v, WallTile))
        {
            bool rightCorner = Map.IsTile(v.x + 1, v.y - 1, WallTile) && !Map.IsTile(v.x + 1, v.y, WallTile);
            bool leftCorner = Map.IsTile(v.x - 1, v.y - 1, WallTile) && !Map.IsTile(v.x - 1, v.y, WallTile);
            
            if (rightCorner || leftCorner) return CornerTile;
        }

        return null;
    }
}
