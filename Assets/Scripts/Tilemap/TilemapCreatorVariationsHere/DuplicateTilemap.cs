using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class DuplicateTilemap : DuplicateTilemapBase
{
    [Header("Tile References")]
    public RuleTile ReferenceTile;
    public RuleTile NewTile;

    protected override bool OnCreation() {return true;}

    protected override TileBase GetTile(Vector3Int v) {
        if (ReferenceTile == null && NewTile == null) return Map.GetTile(v);
        if (ReferenceTile == null) return !Map.IsNull(v) ? NewTile : null;
        if (NewTile == null) return Map.IsTile(v, ReferenceTile) ? ReferenceTile : null;
        return Map.IsTile(v, ReferenceTile) ? NewTile : null;
    }
}