using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class DuplicateCertainTilemap : DuplicateTilemapBase
{
    [Header("Tile References")]
    public Tilemap tilemap;

    protected override bool OnCreation() {
        if (tilemap==null) return false;
        tilemap.CompressBounds();
        bounds = tilemap.cellBounds;
        return true;
    }
    protected override TileBase GetTile(Vector3Int v) {
        return tilemap.GetTile(v);
    }
}