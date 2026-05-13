using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates top/corner tiles for walls based on the main tilemap layout.
/// Optimized to work with the new Map system.
/// </summary>
public class TilemapTop : MonoBehaviour
{
    [Header("Tile References")]
    public RuleTile WallTile;
    public RuleTile TopTile, CornerTile;
    
    private Tilemap _thisTilemap;
    private bool queued = false;

    void OnValidate()
    {
        _thisTilemap = GetComponent<Tilemap>();
        if (_thisTilemap == null||WallTile == null||TopTile == null||CornerTile == null||!Map.IsInitialized||queued) return;
        queued = true;
        EditorApplication.delayCall += () =>{
            queued = false;
            var bounds = Map.Bounds;
            var positions = new System.Collections.Generic.List<Vector3Int>();
            var tiles = new System.Collections.Generic.List<TileBase>();

            // Process each position in the extended bounds (y+1 for tops)
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax + 1; y++)
                {
                    var position = new Vector3Int(x, y, 0);
                    var tileToPlace = DetermineTileType(x, y);
                    
                    positions.Add(position);
                    tiles.Add(tileToPlace);
                }
            }

            // Apply all tiles at once
            _thisTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            
            Debug.Log($"[TilemapTop] Generated {positions.Count} top tiles in batch mode", this);
        };
    }
    private TileBase DetermineTileType(int x, int y)
    {
        // Check if there's a wall below and no wall at current position
        if (Map.IsTile(x, y - 1, WallTile) && !Map.IsTile(x, y, WallTile))
        {
            return TopTile;
        }
        
        // Check for corner conditions
        if (Map.IsTile(x, y, WallTile))
        {
            bool rightCorner = Map.IsTile(x + 1, y - 1, WallTile) && !Map.IsTile(x + 1, y, WallTile);
            bool leftCorner = Map.IsTile(x - 1, y - 1, WallTile) && !Map.IsTile(x - 1, y, WallTile);
            
            if (rightCorner || leftCorner)
            {
                return CornerTile;
            }
        }
        
        // No special tile needed
        return null;
    }
}
