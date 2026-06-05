using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class DeskTop : DuplicateTilemapBase {
    [Header("Tile References")]
    public RuleTile WallTile;
    public Tile TopTile;

    protected override bool OnCreation() { 
        return WallTile != null && TopTile != null && Map.IsInitialized;
    }
    
    protected override TileBase GetTile(Vector3Int v) {
        if (Map.IsTile(v.x, v.y - 1, WallTile) && Map.IsTile(v, WallTile) && Map.Wall.GetTile(v) && !Map.Wall.GetTile(new Vector3Int(v.x, v.y-1, 0))) return TopTile;
        return null;
    }
}