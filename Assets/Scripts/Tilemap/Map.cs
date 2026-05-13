using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class Map
{
    private static Tilemap _wall;
    private static Tilemap _transparent;
    private static BoundsInt _bounds;

    /// <summary>
    /// Current tilemap reference. Null if not initialized.
    /// </summary>
    public static Tilemap Wall => _wall;
    /// <summary>
    /// Current tilemap reference for transparent objects. Null if not initialized.
    /// </summary>
    public static Tilemap Transparent => _transparent;
    /// <summary>
    /// Checks if both tilemaps are initialized.
    /// </summary>
    public static bool IsInitialized  => _wall!=null&&_transparent!=null;
    /// <summary>
    /// Checks if both tilemaps are initialized.
    /// </summary>
    public static BoundsInt Bounds  => _bounds;
    public static void SetWall(Tilemap tilemap) {
        if (tilemap == null) {
            Debug.LogError("[Map] Cannot initialize with null tilemap reference.");
            return;
        }

        _wall = tilemap;
        FixBounds();
    }

    public static void SetTransparent(Tilemap tilemap) {
        if (tilemap == null) {
            Debug.LogError("[Map] Cannot initialize with null tilemap reference.");
            return;
        }

        _transparent = tilemap;
        FixBounds();
    }

    private static void FixBounds() {
        if (!IsInitialized) return;
        _wall.CompressBounds();
        _transparent.CompressBounds();
        _bounds = new BoundsInt {
            xMin = Mathf.Min(_wall.cellBounds.xMin,_transparent.cellBounds.xMin),
            xMax = Mathf.Max(_wall.cellBounds.xMax,_transparent.cellBounds.xMax),
            yMin = Mathf.Min(_wall.cellBounds.yMin,_transparent.cellBounds.yMin),
            yMax = Mathf.Max(_wall.cellBounds.yMax,_transparent.cellBounds.yMax)
        };
    }

    public static bool IsTile(Vector3Int v, TileBase tile) {
        if (!IsInitialized) return false;
        return _wall.GetTile(v)==tile||_transparent.GetTile(v)==tile;
    }

    public static bool IsTile(Vector2Int v, TileBase tile) {
        return IsTile(new Vector3Int(v.x, v.y, 0), tile);
    }
    
    public static bool IsTile(int x, int y, TileBase tile) {
        return IsTile(new Vector3Int(x,y,0),tile);
    }
    public static bool IsNull(Vector3Int v) {
        if (!IsInitialized) return false;
        return !(_wall.GetTile(v)||_transparent.GetTile(v));
    }

    public static bool IsNull(Vector2Int v) {
        return IsNull(new Vector3Int(v.x, v.y, 0));
    }
    
    public static bool IsNull(int x, int y) {
        return IsNull(new Vector3Int(x,y,0));
    }
    public static TileBase GetTile(Vector3Int v) {
        if (!IsInitialized) return null;
        if (_wall.GetTile(v)==null) return _transparent.GetTile(v);
        return _wall.GetTile(v);
    }
    public static TileBase GetTile(Vector2Int v) {
        return GetTile(new Vector3Int(v.x, v.y, 0));
    }
    public static TileBase GetTile(int x, int y) {
        return GetTile(new Vector3Int(x, y, 0));
    }
}
