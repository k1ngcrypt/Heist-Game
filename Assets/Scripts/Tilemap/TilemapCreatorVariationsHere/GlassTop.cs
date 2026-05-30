using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class GlassTop : DuplicateTilemapBase
{
    [Header("Tile References")]
    public RuleTile WallTile;
    public Tile TopTile;

    protected override bool OnCreation() { 
        return WallTile != null && TopTile != null && Map.IsInitialized;
    }
    protected override TileBase GetTile(Vector3Int v) {
        if (Map.IsTile(v.x, v.y - 1, WallTile) && !Map.IsTile(v, WallTile) && !Map.IsNull(v) && !Map.IsNull(v.x, v.y-2)) return TopTile;
        return null;
    }
}