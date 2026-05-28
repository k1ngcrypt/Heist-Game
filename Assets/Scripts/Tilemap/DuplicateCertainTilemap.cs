using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class DuplicateCertainTilemap : DuplicateTilemapBase
{
    [Header("Tile References")]
    public Tilemap tilemap;

    protected override void OnCreation() {
        tilemap.CompressBounds();
        bounds = tilemap.cellBounds;
    }
    protected override TileBase GetTile(Vector3Int v) {
        return tilemap.GetTile(v);
    }
}