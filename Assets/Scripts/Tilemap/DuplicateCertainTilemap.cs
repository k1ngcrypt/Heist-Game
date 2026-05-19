using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class DuplicateCertainTilemap : MonoBehaviour
{
    [Header("Tile References")]
    public Tilemap tilemap;
    private Tilemap _thisTilemap;
    private bool queued = false;

    void OnValidate()
    {
        _thisTilemap = GetComponent<Tilemap>();
        if (_thisTilemap == null) return;
        if (tilemap==null||queued) return;
        queued = true;
        EditorApplication.delayCall += () => {
            queued = false;
            tilemap.CompressBounds();
            var bounds = tilemap.cellBounds;
            var positions = new System.Collections.Generic.List<Vector3Int>();
                var tiles = new System.Collections.Generic.List<TileBase>();

            for (int x = bounds.xMin; x < bounds.xMax; x++)
                for (int y = bounds.yMin; y < bounds.yMax + 1; y++) {
                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(tilemap.GetTile(new Vector3Int(x, y)));
                }
            _thisTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
        };
    }
}