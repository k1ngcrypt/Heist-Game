using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class VentWalls : MonoBehaviour
{
    [Header("Tile References")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private RuleTile floorTile;
    [SerializeField] private RuleTile wallTile;
    private Tilemap _thisTilemap;
    private bool queued = false;

    void OnValidate()
    {
        _thisTilemap = GetComponent<Tilemap>();
        if (_thisTilemap == null) return;
        if (floorTilemap==null||floorTile==null||wallTile==null||queued) return;
        queued = true;
        EditorApplication.delayCall += () => {
            queued = false;
            floorTilemap.CompressBounds();
            var tBounds = floorTilemap.cellBounds;
            int xMin = tBounds.xMin, xMax = tBounds.xMax, yMin = tBounds.yMin, yMax = tBounds.yMax;
            BoundsInt bounds = new();
            bool b = true;
            for (int x = xMin; x<=xMax; x++) for (int y = yMin; y<=xMax; y++) {
                    if (floorTilemap.GetTile(new Vector3Int(x,y,0))==floorTile) {
                        if (b) {
                            b=false;
                            bounds.xMin = x;
                            bounds.xMax = x;
                            bounds.yMin = y;
                            bounds.yMax = y; 
                        }
                        bounds.xMin = Mathf.Min(bounds.xMin, x);
                        bounds.xMax = Mathf.Max(bounds.xMax, x);
                        bounds.yMin = Mathf.Min(bounds.yMin, y);
                        bounds.yMax = Mathf.Max(bounds.yMax, y);
                    }
            }
            bounds.xMax+=8;
            bounds.yMax+=8;

            var positions = new System.Collections.Generic.List<Vector3Int>();
            var tiles = new System.Collections.Generic.List<TileBase>();

            for (int x = bounds.xMin-8; x <= bounds.xMax; x++)
                for (int y = bounds.yMin-8; y <= bounds.yMax + 1; y++) {
                    var tile = _thisTilemap.GetTile(new Vector3Int(x, y));
                    if (tile==wallTile||tile==null) {
                        positions.Add(new Vector3Int(x, y, 0));
                        tiles.Add(floorTilemap.GetTile(new Vector3Int(x, y))==floorTile?null:wallTile);
                    }
                }
            _thisTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
        };
    }
}
