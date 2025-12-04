using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class WallFloor : MonoBehaviour
{
    public RuleTile floorWallTile;
    public List<TileBase> walls;
    private Tilemap _thisTilemap;
    void Start()
    {
        _thisTilemap = GetComponent<Tilemap>();
        if (_thisTilemap == null||floorWallTile == null) return;
        StartCoroutine(WaitForMapThenGenerate());
    }
    private System.Collections.IEnumerator WaitForMapThenGenerate()
    {
        int attempts = 0;
        while (!Map.IsInitialized && attempts < 60)
        {
            yield return null;
            attempts++;
        }

        if (!Map.IsInitialized)
        {
            Debug.LogError("[WallFloor] Map system not initialized, cannot generate top tiles!", this);
            yield break;
        }

        GenerateTopTilesBatch();
    }
    private void GenerateTopTilesBatch()
    {
        var bounds = Map.Bounds;
        var positions = new List<Vector3Int>();
        var tiles = new List<TileBase>();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax + 1; y++)
                foreach (TileBase t in walls)
                    if (Map.IsTile(x,y,t)) {
                        positions.Add(new Vector3Int(x, y, 0));
                        tiles.Add(floorWallTile);
                        break;
                    }
        _thisTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
        Debug.Log($"[WallFloor] Generated {positions.Count} top tiles in batch mode", this);
    }
}
