using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Map
{
    public static Tilemap objectTilemap, playerTilemap;
    public static RuleTile nullTile;
    public static int xMin, xMax, yMin, yMax;
    public static void SetTilemap(Tilemap t)
    {
        objectTilemap = t;
        xMin = objectTilemap.cellBounds.xMin;
        xMax = objectTilemap.cellBounds.xMax;
        yMin = objectTilemap.cellBounds.yMin;
        yMax = objectTilemap.cellBounds.yMax;
        nullTile = (RuleTile)Resources.Load("NullTile");
    }
    public static void SetPlayerTilemap(Tilemap t)
    {
        playerTilemap = t;
    }
    public static bool IsTile(Vector3Int v, TileBase tile)
    {
        return objectTilemap.GetTile(v) == tile;
    }
    public static bool IsTile(int x, int y, TileBase tile)
    {
        return IsTile(new Vector3Int(x, y, 0), tile);
    }
    public static bool IsFilled(Vector3Int v)
    {
        return objectTilemap.GetTile(v);
    }
    public static bool IsFilled(int x, int y)
    {
        return IsFilled(new Vector3Int(x, y, 0));
    }
    public static void SetTile(Vector3Int v, TileBase tile)
    {
        objectTilemap.SetTile(v, tile);
    }
    public static void SetTile(int x, int y, TileBase tile)
    {
        SetTile(new Vector3Int(x, y, 0), tile);
    }
}
