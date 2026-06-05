using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class NotWindows : DuplicateTilemapBase
{
    [Header("Tile References")]
    public RuleTile Windows;

    protected override bool OnCreation() {
        if (Windows==null||!Map.IsInitialized) return false;
        Map.Transparent.CompressBounds();
        bounds = Map.Transparent.cellBounds;
        
        return true;
    }

    protected override TileBase GetTile(Vector3Int v) {
        if (Map.Transparent.GetTile(v)!=Windows) return Map.Transparent.GetTile(v);
        return null;
    }
}