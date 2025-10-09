using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// High-performance automatic floor generator for Tilemaps.
/// Efficiently fills large areas with floor tiles using batch operations.
/// Integrates with the Map system for optimal performance.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class AutoFloor : MonoBehaviour
{
    #region Inspector Fields
    [Header("Tile Settings")]
    [SerializeField] private RuleTile floorTile;
    [Tooltip("If true, will use Map system bounds instead of manual bounds")]
    [SerializeField] private bool useMapBounds = false;

    [Header("Manual Map Bounds")]
    [SerializeField] private Vector2Int mapMin = new Vector2Int(-50, -50);
    [SerializeField] private Vector2Int mapMax = new Vector2Int(50, 50);

    [Header("Performance Settings")]
    [Tooltip("Maximum tiles to process per frame (0 = process all at once)")]
    [SerializeField] private int tilesPerFrame = 0;
    [Tooltip("If true, will only fill empty tiles")]

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    #endregion

    #region Private Fields
    private Tilemap _tilemap;
    private bool _isGenerating = false;
    private System.Diagnostics.Stopwatch _performanceTimer;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initialize components early.
    /// </summary>
    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        _performanceTimer = new System.Diagnostics.Stopwatch();
        
        if (_tilemap == null)
        {
            Debug.LogError("[AutoFloor] No Tilemap component found!", this);
        }
    }

    /// <summary>
    /// Generate floor after all initialization is complete.
    /// </summary>
    private void Start()
    {
        if (floorTile == null)
        {
            Debug.LogError("[AutoFloor] No floor tile assigned. Please set a RuleTile in the inspector.", this);
            return;
        }
        GenerateFloor();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Manually trigger floor generation.
    /// </summary>
    public void GenerateFloor()
    {
        if (_isGenerating)
        {
            Debug.LogWarning("[AutoFloor] Already generating floor, please wait.", this);
            return;
        }

        if (floorTile == null)
        {
            Debug.LogError("[AutoFloor] Cannot generate floor without a floor tile assigned.", this);
            return;
        }

        StartCoroutine(GenerateFloorCoroutine());
    }

    /// <summary>
    /// Stop current generation process.
    /// </summary>
    public void StopGeneration()
    {
        _isGenerating = false;
        StopAllCoroutines();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Wait for Map system initialization before generating.
    /// </summary>
    private IEnumerator WaitForMapSystemThenGenerate()
    {
        int attempts = 0;
        const int maxAttempts = 60; // Wait up to 1 second at 60 FPS

        while (!Map.IsInitialized && attempts < maxAttempts)
        {
            yield return null;
            attempts++;
        }

        if (!Map.IsInitialized)
        {
            Debug.LogWarning("[AutoFloor] Map system not initialized after waiting, using manual bounds.", this);
        }

        GenerateFloor();
    }

    /// <summary>
    /// Main floor generation coroutine with performance optimization.
    /// </summary>
    private IEnumerator GenerateFloorCoroutine()
    {
        _isGenerating = true;
        _performanceTimer.Restart();

        // Determine bounds to use
        Vector2Int actualMin, actualMax;
        if (useMapBounds&&Map.IsInitialized)
        {
            actualMin = (Vector2Int)Map.Bounds.min;
            actualMax = (Vector2Int)Map.Bounds.max;
        } else
        {
            actualMin = mapMin;
            actualMax = mapMax;
        }

        var width = actualMax.x - actualMin.x;
        var height = actualMax.y - actualMin.y;
        var totalTiles = width * height;

        if (showDebugInfo)
            Debug.Log($"[AutoFloor] Generating floor: {width}x{height} = {totalTiles} tiles", this);

        // Choose generation method based on performance settings
        if (tilesPerFrame <= 0)
        {
            // Batch generation - fastest for smaller areas
            yield return StartCoroutine(BatchGeneration(actualMin, actualMax, width, height));
        }
        else
        {
            // Progressive generation - better for large areas or to avoid frame drops
            yield return StartCoroutine(ProgressiveGeneration(actualMin, actualMax));
        }

        _performanceTimer.Stop();
        _isGenerating = false;

        if (showDebugInfo)
        {
            Debug.Log($"[AutoFloor] Floor generation completed in {_performanceTimer.ElapsedMilliseconds}ms", this);
        }
    }

    /// <summary>
    /// Generate all tiles at once using batch operations.
    /// </summary>
    private IEnumerator BatchGeneration(Vector2Int min, Vector2Int max, int width, int height)
    {
        var positions = new Vector3Int[width * height];
        var tiles = new TileBase[positions.Length];
        int validTileCount = 0;

        // Prepare tile data
        for (int x = min.x; x < max.x; x++)
        {
            for (int y = min.y; y < max.y; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                
                positions[validTileCount] = pos;
                tiles[validTileCount] = floorTile;
                validTileCount++;
            }
        }

        // Resize arrays to actual count
        if (validTileCount < positions.Length)
        {
            System.Array.Resize(ref positions, validTileCount);
            System.Array.Resize(ref tiles, validTileCount);
        }

        // Apply tiles using the most efficient method available
        _tilemap.SetTiles(positions, tiles);

        yield return null;
    }

    /// <summary>
    /// Generate tiles progressively over multiple frames.
    /// </summary>
    private IEnumerator ProgressiveGeneration(Vector2Int min, Vector2Int max)
    {
        int tilesProcessed = 0;

        for (int x = min.x; x < max.x && _isGenerating; x++)
        {
            for (int y = min.y; y < max.y && _isGenerating; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                _tilemap.SetTile(pos, floorTile);

                tilesProcessed++;

                // Yield periodically to maintain frame rate
                if (tilesProcessed >= tilesPerFrame)
                {
                    tilesProcessed = 0;
                    yield return null;
                }
            }
        }
    }
    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only floor generation for design-time use.
    /// </summary>
    [ContextMenu("Generate Floor")]
    private void GenerateFloorInEditor()
    {
        if (Application.isPlaying)
        {
            GenerateFloor();
            return;
        }

        if (floorTile == null)
        {
            Debug.LogError("[AutoFloor] No floor tile assigned for editor generation.", this);
            return;
        }

        if (_tilemap == null)
            _tilemap = GetComponent<Tilemap>();

        _performanceTimer = _performanceTimer ?? new System.Diagnostics.Stopwatch();
        _performanceTimer.Restart();

        // Use manual bounds for editor generation
        var width = mapMax.x - mapMin.x;
        var height = mapMax.y - mapMin.y;
        var positions = new Vector3Int[width * height];
        var tiles = new TileBase[positions.Length];
        int index = 0;

        for (int x = mapMin.x; x < mapMax.x; x++)
        {
            for (int y = mapMin.y; y < mapMax.y; y++)
            {
                positions[index] = new Vector3Int(x, y, 0);
                tiles[index] = floorTile;
                index++;
            }
        }

        _tilemap.SetTiles(positions, tiles);
        _performanceTimer.Stop();

        Debug.Log($"[AutoFloor] Editor generation completed in {_performanceTimer.ElapsedMilliseconds}ms ({positions.Length} tiles)", this);
    }

    /// <summary>
    /// Editor validation and setup.
    /// </summary>
    private void OnValidate()
    {
        // Ensure map bounds are valid
        if (mapMin.x >= mapMax.x)
            mapMax.x = mapMin.x + 1;
        if (mapMin.y >= mapMax.y)
            mapMax.y = mapMin.y + 1;

        // Clamp tiles per frame to reasonable values
        if (tilesPerFrame < 0)
            tilesPerFrame = 0;
        else if (tilesPerFrame > 10000)
            tilesPerFrame = 10000;
    }

    /// <summary>
    /// Draw bounds in scene view for visualization.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        Vector2Int actualMin, actualMax;
        if (useMapBounds&&Map.IsInitialized)
        {
            actualMin = (Vector2Int)Map.Bounds.min;
            actualMax = (Vector2Int)Map.Bounds.max;
        } else
        {
            actualMin = mapMin;
            actualMax = mapMax;
        }

        Gizmos.color = _isGenerating ? Color.yellow : Color.green;
        var center = new Vector3((actualMin.x + actualMax.x) * 0.5f, (actualMin.y + actualMax.y) * 0.5f, 0);
        var size = new Vector3(actualMax.x - actualMin.x, actualMax.y - actualMin.y, 0);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
