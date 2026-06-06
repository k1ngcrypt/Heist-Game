using System;
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

    protected override bool OnCreation() {
        _thisTilemap.CompressBounds();
        bounds.xMin = Mathf.Min(Map.Bounds.xMin, _thisTilemap.cellBounds.xMin);
        bounds.xMax = Mathf.Max(Map.Bounds.xMax, _thisTilemap.cellBounds.xMax);
        bounds.yMin = Mathf.Min(Map.Bounds.yMin, _thisTilemap.cellBounds.yMin);
        bounds.yMax = Mathf.Max(Map.Bounds.yMax, _thisTilemap.cellBounds.yMax);
        return ReferenceTiles!=null && ReferenceTiles.Count > 0 && NewTile != null;
    }

    protected override TileBase GetTile(Vector3Int v) {
        foreach (RuleTile t in ReferenceTiles) if (Map.IsTile(v, t)) return NewTile;
        return null;
    }
}