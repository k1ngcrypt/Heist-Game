using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Map
{
    public static Tilemap tilemap;
    public static RuleTile nullTile;
    public static int xMin, xMax, yMin, yMax;
    public static void SetTilemap(Tilemap t)
    {
        tilemap = t;
        xMin = tilemap.cellBounds.xMin;
        xMax = tilemap.cellBounds.xMax;
        yMin = tilemap.cellBounds.yMin;
        yMax = tilemap.cellBounds.yMax;
        nullTile = (RuleTile)Resources.Load("NullTile");
    }
    public static bool IsTile(Vector3Int v, TileBase tile)
    {
        return tilemap.GetTile(v) == tile;
    }
    public static bool IsTile(int x, int y, TileBase tile)
    {
        return IsTile(new Vector3Int(x, y, 0), tile);
    }
    public static bool IsFilled(Vector3Int v)
    {
        return tilemap.GetTile(v);
    }
    public static bool IsFilled(int x, int y)
    {
        return IsFilled(new Vector3Int(x, y, 0));
    }
    public static void SetTile(Vector3Int v, TileBase tile)
    {
        tilemap.SetTile(v, tile);
    }
    public static void SetTile(int x, int y, TileBase tile)
    {
        SetTile(new Vector3Int(x, y, 0), tile);
    }
}
