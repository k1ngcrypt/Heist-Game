using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class DuplicateFloor : DuplicateTilemapBase
{
    [Header("Tile References")]
    [SerializeField] private Tilemap Floor;
    [SerializeField] private Tilemap Base;

    protected override bool OnCreation() {
        if (Floor==null||Base==null) return false;
        Floor.CompressBounds();
        bounds = Floor.cellBounds;
        Base.CompressBounds();
        bounds.xMin = Mathf.Min(bounds.xMin, Base.cellBounds.xMin);
        bounds.xMax = Mathf.Max(bounds.xMax, Base.cellBounds.xMax);
        bounds.yMin = Mathf.Min(bounds.yMin, Base.cellBounds.yMin);
        bounds.yMax = Mathf.Max(bounds.yMax, Base.cellBounds.yMax);
        return true;
    }
    protected override TileBase GetTile(Vector3Int v) {
        return Map.IsNull(v) ? (Floor.GetTile(v)==null ? Base.GetTile(v) : Floor.GetTile(v)) : null;
    }
}