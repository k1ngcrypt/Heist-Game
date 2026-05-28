using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class ShadowWallCreatorTilemap : DuplicateTilemapBase
{
    [Header("Tile References")]
    public List<RuleTile> ReferenceTiles;
    public RuleTile NewTile;

    protected override void OnCreation() {}

    protected override TileBase GetTile(Vector3Int v) {
        if (ReferenceTiles==null || ReferenceTiles.Count==0 || NewTile == null) return null;
        foreach (RuleTile t in ReferenceTiles) if (Map.IsTile(v, t)) return NewTile;
        return null;
    }
}