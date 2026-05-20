using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class GlassTop : MonoBehaviour
{
    [Header("Tile References")]
    public RuleTile WallTile;
    public Tile TopTile;
    private Tilemap _thisTilemap;
    private bool queued = false;

    void OnValidate()
    { 
        _thisTilemap = GetComponent<Tilemap>();
        if (_thisTilemap == null||WallTile == null || TopTile == null || queued) return;
        if (!Map.IsInitialized) return;
        queued = true;
        EditorApplication.delayCall += () => {
            queued = false;
            var bounds = Map.Bounds;
            var positions = new System.Collections.Generic.List<Vector3Int>();
            var tiles = new System.Collections.Generic.List<TileBase>();
            Debug.Log(bounds.xMin+" -> "+bounds.xMax+" || "+bounds.yMin+" -> "+bounds.yMax);

            for (int x = bounds.xMin; x < bounds.xMax; x++)
                for (int y = bounds.yMin; y < bounds.yMax + 1; y++) {
                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(Map.IsTile(x, y - 1, WallTile) && !Map.IsTile(x, y, WallTile) && !Map.IsNull(x, y) && !Map.IsNull(x, y-2) ? TopTile : null);
                }
            _thisTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            Debug.Log($"[GlassTop] Generated {positions.Count} top tiles in batch mode", this);
        };
    }
}