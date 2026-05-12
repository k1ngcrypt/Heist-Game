using UnityEngine;
using UnityEngine.Tilemaps;

public class DuplicateTilemap : MonoBehaviour
{
    [Header("Tile References")]
    public RuleTile ReferenceTile;
    public RuleTile NewTile;
    private Tilemap _thisTilemap;

    void Start()
    {
        _thisTilemap = GetComponent<Tilemap>();
        if (_thisTilemap == null) return;
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
            Debug.LogError("[Duplicate] Map system not initialized, cannot generate top tiles!", this);
            yield break;
        }

        GenerateTopTilesBatch();
    }
    private void GenerateTopTilesBatch()
    {
        var bounds = Map.Bounds;
        var positions = new System.Collections.Generic.List<Vector3Int>();
        var tiles = new System.Collections.Generic.List<TileBase>();

        if (ReferenceTile == null || NewTile == null)
            for (int x = bounds.xMin; x < bounds.xMax; x++)
                for (int y = bounds.yMin; y < bounds.yMax + 1; y++)
                {
                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(Map.GetTile(new Vector3Int(x,y)));
                }
        else
            for (int x = bounds.xMin; x < bounds.xMax; x++)
                for (int y = bounds.yMin; y < bounds.yMax + 1; y++)
                {
                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(Map.IsTile(x, y, ReferenceTile) ? NewTile : null);
                }
        _thisTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
        Debug.Log($"[DuplicateTilemapp] Generated {positions.Count} top tiles in batch mode", this);
    }
}