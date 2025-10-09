using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// High-performance static tilemap manager with optimized operations and bounds checking.
/// Provides centralized access to tilemap operations with memory-efficient implementations.
/// </summary>
public static class Map
{
    #region Private Fields
    private static Tilemap _tilemap;
    private static RuleTile _nullTile;
    private static BoundsInt _bounds;
    private static readonly Dictionary<Vector3Int, TileBase> _tileCache = new Dictionary<Vector3Int, TileBase>();
    private static bool _isInitialized = false;
    #endregion

    #region Public Properties
    /// <summary>
    /// Current tilemap reference. Null if not initialized.
    /// </summary>
    public static Tilemap Tilemap => _tilemap;
    
    /// <summary>
    /// Null tile reference for empty tile operations.
    /// </summary>
    public static RuleTile NullTile => _nullTile;
    
    /// <summary>
    /// Current tilemap bounds.
    /// </summary>
    public static BoundsInt Bounds => _bounds;
    
    /// <summary>
    /// Whether the Map has been properly initialized.
    /// </summary>
    public static bool IsInitialized => _isInitialized && _tilemap != null;
    
    /// <summary>
    /// Minimum X coordinate of the tilemap bounds.
    /// </summary>
    public static int XMin => _bounds.xMin;
    
    /// <summary>
    /// Maximum X coordinate of the tilemap bounds.
    /// </summary>
    public static int XMax => _bounds.xMax;
    
    /// <summary>
    /// Minimum Y coordinate of the tilemap bounds.
    /// </summary>
    public static int YMin => _bounds.yMin;
    
    /// <summary>
    /// Maximum Y coordinate of the tilemap bounds.
    /// </summary>
    public static int YMax => _bounds.yMax;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the Map with a tilemap reference and loads required resources.
    /// </summary>
    /// <param name="tilemap">The tilemap to manage</param>
    /// <param name="nullTileResourcePath">Optional custom path for null tile resource</param>
    public static void SetTilemap(Tilemap tilemap, string nullTileResourcePath = "NullTile")
    {
        // Load the RuleTile from resources and delegate to the RuleTile overload
        RuleTile loaded = Resources.Load<RuleTile>(nullTileResourcePath);
        if (loaded == null)
        {
            Debug.LogWarning($"[Map] Could not load null tile from path: {nullTileResourcePath}");
        }

        SetTilemap(tilemap, loaded);
    }

    /// <summary>
    /// Initializes the Map with a tilemap reference and an inspector-assigned null tile.
    /// </summary>
    /// <param name="tilemap">The tilemap to manage</param>
    /// <param name="nullTile">A RuleTile to use as the null/empty tile</param>
    public static void SetTilemap(Tilemap tilemap, RuleTile nullTile)
    {
        if (tilemap == null)
        {
            Debug.LogError("[Map] Cannot initialize with null tilemap reference.");
            return;
        }

        _tilemap = tilemap;
        RefreshBounds();

        _nullTile = nullTile;

        _isInitialized = true;
        ClearCache();
        
        Debug.Log($"[Map] Initialized with bounds: {_bounds}");
    }

    /// <summary>
    /// Refreshes the cached bounds from the current tilemap.
    /// </summary>
    public static void RefreshBounds()
    {
        if (_tilemap != null)
           _bounds = _tilemap.cellBounds;
    }

    /// <summary>
    /// Clears the internal tile cache. Use when tilemap changes externally.
    /// </summary>
    public static void ClearCache()
    {
        _tileCache.Clear();
    }
    #endregion

    #region Bounds Checking
    /// <summary>
    /// Checks if the given position is within tilemap bounds.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <returns>True if position is within bounds</returns>
    public static bool IsInBounds(Vector3Int position)
    {
        return _bounds.Contains(position);
    }

    /// <summary>
    /// Checks if the given coordinates are within tilemap bounds.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>True if coordinates are within bounds</returns>
    public static bool IsInBounds(int x, int y)
    {
        return x >= _bounds.xMin && x < _bounds.xMax && 
               y >= _bounds.yMin && y < _bounds.yMax;
    }
    #endregion

    #region Tile Query Operations
    /// <summary>
    /// Checks if the tile at the given position matches the specified tile.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="tile">Tile to compare against</param>
    /// <returns>True if tiles match, false otherwise</returns>
    public static bool IsTile(Vector3Int position, TileBase tile)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[Map] Not initialized. Call SetTilemap() first.");
            return false;
        }

        if (!IsInBounds(position))
        {
            return false;
        }

        return GetTileInternal(position) == tile;
    }

    /// <summary>
    /// Checks if the tile at the given coordinates matches the specified tile.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="tile">Tile to compare against</param>
    /// <returns>True if tiles match, false otherwise</returns>
    public static bool IsTile(int x, int y, TileBase tile)
    {
        return IsTile(new Vector3Int(x, y, 0), tile);
    }

    /// <summary>
    /// Checks if there is a tile at the given position.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <returns>True if position has a tile, false if empty</returns>
    public static bool IsFilled(Vector3Int position)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[Map] Not initialized. Call SetTilemap() first.");
            return false;
        }
        return GetTileInternal(position) != null;
    }

    /// <summary>
    /// Checks if there is a tile at the given coordinates.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>True if position has a tile, false if empty</returns>
    public static bool IsFilled(int x, int y)
    {
        return IsFilled(new Vector3Int(x, y, 0));
    }

    /// <summary>
    /// Gets the tile at the specified position.
    /// </summary>
    /// <param name="position">Position to get tile from</param>
    /// <returns>The tile at the position, or null if empty/out of bounds</returns>
    public static TileBase GetTile(Vector3Int position)
    {
        if (!IsInitialized) return null;
        return GetTile(position);
    }

    /// <summary>
    /// Gets the tile at the specified coordinates.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>The tile at the position, or null if empty/out of bounds</returns>
    public static TileBase GetTile(int x, int y)
    {
        return GetTile(new Vector3Int(x, y, 0));
    }
    #endregion

    #region Tile Modification Operations
    /// <summary>
    /// Sets a tile at the given position.
    /// </summary>
    /// <param name="position">Position to set tile at</param>
    /// <param name="tile">Tile to set (null to clear)</param>
    /// <returns>True if tile was set successfully</returns>
    public static bool SetTile(Vector3Int position, TileBase tile)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[Map] Not initialized. Call SetTilemap() first.");
            return false;
        }
        _tilemap.SetTile(position, tile);
        
        // Update cache
        if (tile == null)
            _tileCache.Remove(position);
        else _tileCache[position] = tile;

        return true;
    }

    /// <summary>
    /// Sets a tile at the given coordinates.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="tile">Tile to set (null to clear)</param>
    /// <returns>True if tile was set successfully</returns>
    public static bool SetTile(int x, int y, TileBase tile)
    {
        return SetTile(new Vector3Int(x, y, 0), tile);
    }

    /// <summary>
    /// Sets multiple tiles at once for better performance.
    /// </summary>
    /// <param name="positions">Array of positions</param>
    /// <param name="tiles">Array of tiles (must match positions length)</param>
    /// <returns>True if all tiles were set successfully</returns>
    public static bool SetTiles(Vector3Int[] positions, TileBase[] tiles)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[Map] Not initialized. Call SetTilemap() first.");
            return false;
        }

        if (positions == null || tiles == null || positions.Length != tiles.Length)
        {
            Debug.LogError("[Map] Invalid arrays provided to SetTiles()");
            return false;
        }

        _tilemap.SetTiles(positions, tiles);

        // Update cache
        for (int i = 0; i < positions.Length; i++)
        {
            if (tiles[i] == null)
                _tileCache.Remove(positions[i]);
            else _tileCache[positions[i]] = tiles[i];
        }

        return true;
    }

    /// <summary>
    /// Fills a rectangular area with the specified tile.
    /// </summary>
    /// <param name="area">Area to fill</param>
    /// <param name="tile">Tile to fill with</param>
    /// <returns>True if area was filled successfully</returns>
    public static bool FloodFill(BoundsInt area, TileBase tile)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[Map] Not initialized. Call SetTilemap() first.");
            return false;
        }

        _tilemap.FloodFill(area.position, tile);
        ClearCache(); // Clear cache as we can't efficiently update it for flood fill
        return true;
    }
    #endregion

    #region Private Helpers
    /// <summary>
    /// Internal method to get tile with optional caching.
    /// </summary>
    /// <param name="position">Position to get tile from</param>
    /// <returns>The tile at the position</returns>
    private static TileBase GetTileInternal(Vector3Int position)
    {
        // Try cache first for frequently accessed tiles
        if (_tileCache.TryGetValue(position, out TileBase cachedTile))
        {
            return cachedTile;
        }

        // Get from tilemap and cache result
        TileBase tile = _tilemap.GetTile(position);
        if (tile != null)
        {
            _tileCache[position] = tile;
        }

        return tile;
    }
    #endregion
}
