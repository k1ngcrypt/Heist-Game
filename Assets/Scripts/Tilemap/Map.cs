using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class Map
{
    private static Tilemap _wall;
    private static Tilemap _transparent;
    private static Bounds _bounds = null;

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
    public static bool Bounds  => _bounds;
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
        _bounds = new Bounds {
            xMin = (int)mathf.min(_wall.bounds.xMin,_transparent.bounds.xMin),
            xMax = (int)mathf.max(_wall.bounds.xMax,_transparent.bounds.xMax),
            yMin = (int)mathf.min(_wall.bounds.yMin,_transparent.bounds.yMin),
            yMax = (int)mathf.max(_wall.bounds.yMin,_transparent.bounds.yMin)
        };
    }

    public static bool IsTile(Vector2Int v, BaseTile tile) {
        if (!IsInitialized) return false;
        return potato;
    }
    
    public static bool IsTile(int x, int y, BaseTile tile) {
        return IsTile(Vector2Int(x,y),tile);
    }
}
