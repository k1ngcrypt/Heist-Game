using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class DuplicateTilemap : MonoBehaviour
{
    [Header("Tile References")]
    public RuleTile ReferenceTile;
    public RuleTile NewTile;
    private Tilemap _thisTilemap;
    private bool queued = false;

    void OnValidate()
    {
        _thisTilemap = GetComponent<Tilemap>();
        if (_thisTilemap == null) return;
        if (!Map.IsInitialized||queued) return;
        queued = true;
        EditorApplication.delayCall += () => {
            queued = false;
            var bounds = Map.Bounds;
            var positions = new System.Collections.Generic.List<Vector3Int>();
                var tiles = new System.Collections.Generic.List<TileBase>();

            if (ReferenceTile == null && NewTile == null)
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                    for (int y = bounds.yMin; y < bounds.yMax + 1; y++)
                    {
                        positions.Add(new Vector3Int(x, y, 0));
                        tiles.Add(Map.GetTile(new Vector3Int(x,y)));
                    }
            else if (ReferenceTile == null)
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                    for (int y = bounds.yMin; y < bounds.yMax + 1; y++)
                    {
                        positions.Add(new Vector3Int(x, y, 0));
                        tiles.Add(!Map.IsNull(new Vector3Int(x,y)) ? NewTile : null);
                        //Debug.Log("Checked tile at " + new Vector3Int(x,y) + ": " + (Map.IsNull(new Vector3Int(x,y)) ? "null" : "not null"));
                    }
            else if (NewTile == null)
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                    for (int y = bounds.yMin; y < bounds.yMax + 1; y++)
                    {
                        positions.Add(new Vector3Int(x, y, 0));
                        tiles.Add(Map.IsTile(new Vector3Int(x,y), ReferenceTile) ? ReferenceTile : null);
                    }
            else
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                    for (int y = bounds.yMin; y < bounds.yMax + 1; y++)
                    {
                        positions.Add(new Vector3Int(x, y, 0));
                        tiles.Add(Map.IsTile(x, y, ReferenceTile) ? NewTile : null);
                    }
            _thisTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            Debug.Log("[DuplicateTilemap] Duplicated tilemap with " + tiles.Count + " tiles.");
        };
    }
}