using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates top/corner tiles for walls based on the main tilemap layout.
/// Optimized to work with the new Map system.
/// </summary>
public class TilemapTop : MonoBehaviour
{
    [Header("Tile References")]
    public RuleTile WallTile, TopTile, CornerTile;
    
    [Header("Performance")]
    [SerializeField] private bool useProgressiveGeneration = false;
    [SerializeField] private int tilesPerFrame = 1000;
    
    private Tilemap _thisTilemap;
    private bool _isGenerating = false;

    void Start()
    {
        _thisTilemap = GetComponent<Tilemap>();
        
        if (_thisTilemap == null)
        {
            Debug.LogError("[TilemapTop] No Tilemap component found!", this);
            return;
        }

        if (!ValidateConfiguration()) return;

        // Wait for Map system to be ready
        StartCoroutine(WaitForMapThenGenerate());
    }

    /// <summary>
    /// Validates that all required tiles are assigned.
    /// </summary>
    private bool ValidateConfiguration()
    {
        if (WallTile == null)
        {
            Debug.LogError("[TilemapTop] WallTile not assigned!", this);
            return false;
        }
        if (TopTile == null)
        {
            Debug.LogError("[TilemapTop] TopTile not assigned!", this);
            return false;
        }
        if (CornerTile == null)
        {
            Debug.LogError("[TilemapTop] CornerTile not assigned!", this);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Wait for Map system initialization before generating.
    /// </summary>
    private System.Collections.IEnumerator WaitForMapThenGenerate()
    {
        int attempts = 0;
        const int maxAttempts = 60;

        while (!Map.IsInitialized && attempts < maxAttempts)
        {
            yield return null;
            attempts++;
        }

        if (!Map.IsInitialized)
        {
            Debug.LogError("[TilemapTop] Map system not initialized, cannot generate top tiles!", this);
            yield break;
        }

        if (useProgressiveGeneration) yield return StartCoroutine(GenerateTopTilesProgressive());
        else GenerateTopTilesBatch();
    }

    /// <summary>
    /// Generate all top tiles at once using batch operations.
    /// </summary>
    private void GenerateTopTilesBatch()
    {
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
    }

    /// <summary>
    /// Generate top tiles progressively over multiple frames.
    /// </summary>
    private System.Collections.IEnumerator GenerateTopTilesProgressive()
    {
        _isGenerating = true;
        var bounds = Map.Bounds;
        int tilesProcessed = 0;
        int totalTiles = 0;

        for (int x = bounds.xMin; x < bounds.xMax && _isGenerating; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax + 1 && _isGenerating; y++)
            {
                var position = new Vector3Int(x, y, 0);
                var tileToPlace = DetermineTileType(x, y);
                
                _thisTilemap.SetTile(position, tileToPlace);
                
                tilesProcessed++;
                totalTiles++;

                // Yield periodically to maintain frame rate
                if (tilesProcessed >= tilesPerFrame)
                {
                    tilesProcessed = 0;
                    yield return null;
                }
            }
        }

        _isGenerating = false;
        Debug.Log($"[TilemapTop] Generated {totalTiles} top tiles progressively", this);
    }

    /// <summary>
    /// Determines what type of tile should be placed at the given coordinates.
    /// </summary>
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

    /// <summary>
    /// Public method to regenerate top tiles.
    /// </summary>
    public void RegenerateTopTiles()
    {
        if (_isGenerating)
        {
            Debug.LogWarning("[TilemapTop] Already generating, please wait.", this);
            return;
        }

        if (!Map.IsInitialized)
        {
            Debug.LogError("[TilemapTop] Map system not initialized!", this);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(WaitForMapThenGenerate());
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor context menu for manual generation.
    /// </summary>
    [ContextMenu("Generate Top Tiles")]
    private void GenerateInEditor()
    {
        if (Application.isPlaying)
            RegenerateTopTiles();
        else Debug.LogWarning("[TilemapTop] Editor generation requires Play mode for Map system access.", this);
    }
#endif
}
