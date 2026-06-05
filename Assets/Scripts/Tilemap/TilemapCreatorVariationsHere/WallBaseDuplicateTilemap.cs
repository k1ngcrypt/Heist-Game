using UnityEngine;
using UnityEngine.Tilemaps;

public class WallBaseDuplicateTilemap : DuplicateTilemapBase
{
    [Header("Tile References")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private RuleTile baseTop;
    [SerializeField] private RuleTile baseWall;

    protected override bool OnCreation() {
        if (tilemap==null||baseTop==null||baseWall==null) return false;
        tilemap.CompressBounds();
        bounds = tilemap.cellBounds;
        bounds.yMin--;
        return true;
    }
    protected override TileBase GetTile(Vector3Int v) {
        if (tilemap.GetTile(v)!=null) return baseTop;
        if (tilemap.GetTile(v+Vector3Int.up)!=null) return baseWall;
        return null;
    }
}