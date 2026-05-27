using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

public static class Map
{
    [SerializeField] private static Tilemap _wall;
    [SerializeField] private static Tilemap _transparent;
    [SerializeField] private static BoundsInt _bounds;
    [SerializeField] private static GameObject _player;
    [SerializeField] public static List<Vector2> layerLocations;
    [SerializeField] private static List<BoundsInt> _layerBounds;

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
    /// <summary>
    /// The current player reference. Null if not initialized.
    /// </summary>
    public static GameObject Player => _player;
    /// <summary>
    /// The bounds for each layer.
    /// </summary>
    public static List<BoundsInt> LayerBounds => _layerBounds;
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
    

    public static void SetPlayer(GameObject player) {
        if (player == null) {
            Debug.LogError("[Map] Cannot set player to null reference.");
            return;
        }
        _player = player;
    }
    public static int currentLayer()
    {
        if (!IsInitialized||_player == null||layerLocations.Count==0) return 0;
        Vector2 pos = (Vector2) _player.transform.position;
        int l = 0;
        float d = Vector2.Distance(layerLocations[0], pos);
        for (int i = 1; i < layerLocations.Count; i++) {
            float distance = Vector2.Distance(layerLocations[i], pos);
            if (distance < d) {
                d = distance;
                l = i;
            }
        }
        return l;
    }
    
    public static void ReloadLayerBounds() {
        //Debug.Log("SCREAAAAAAAAAAAAAAAAAAMSSSSSSS");
        if (layerLocations.Count==0||!IsInitialized) return;
        _layerBounds = new List<BoundsInt>();
        for (int i = 0; i<layerLocations.Count; i++) {
            BoundsInt bounds = new();
            bool b = true;
            Vector2 location = layerLocations[i];
            for (int x = -100; x<101; x++) for (int y = -100; y<101; y++) {
                    int xx = (int)(x+location.x), yy = (int)(y+location.y);
                    if (!IsNull(xx,yy)) {
                        if (b) {
                            b=false;
                            bounds.xMin = xx;
                            bounds.xMax = xx;
                            bounds.yMin = yy;
                            bounds.yMax = yy; 
                        }
                        bounds.xMin = Mathf.Min(bounds.xMin, xx);
                        bounds.xMax = Mathf.Max(bounds.xMax, xx);
                        bounds.yMin = Mathf.Min(bounds.yMin, yy);
                        bounds.yMax = Mathf.Max(bounds.yMax, yy);
                    }
            }
            _layerBounds.Add(bounds);
            //Debug.Log("AHAHAHAAAAAAAAAAAA");
        }
        //Debug.Log("SCREAAAAAAAAMED "+_layerBounds.Count + " " + layerLocations.Count + " " + LayerBounds.Count);
    }
    public static void SetLayerBounds(List<BoundsInt> l) {
        _layerBounds = l;
    }
}
