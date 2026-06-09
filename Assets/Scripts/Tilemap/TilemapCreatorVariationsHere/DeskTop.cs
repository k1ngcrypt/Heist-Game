using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
class TileDuo {
    public TileBase inputTile;
    public TileBase outputTile;
}

[RequireComponent(typeof(Tilemap))]
public class DeskTop : DuplicateTilemapBase {
    [Header("Tile References")]
    [SerializeField] private List<TileDuo> deskStyleTiles;
    [SerializeField] private List<TileDuo> topStyleTiles;

    protected override bool OnCreation() { 
        return ((deskStyleTiles != null && deskStyleTiles.Count > 0) || (topStyleTiles != null && topStyleTiles.Count > 0)) && Map.IsInitialized;
    }
    
    protected override TileBase GetTile(Vector3Int v) {
        if (deskStyleTiles != null)
            foreach (var tileDuo in deskStyleTiles)
                if (Map.IsTile(v.x, v.y - 1, tileDuo.inputTile) && Map.IsTile(v, tileDuo.inputTile) && Map.Wall.GetTile(v) && !Map.Wall.GetTile(new Vector3Int(v.x, v.y-1, 0))) 
                    return tileDuo.outputTile;
        if (topStyleTiles != null)
            foreach (var tileDuo in topStyleTiles)
                if (Map.IsTile(v.x, v.y - 1, tileDuo.inputTile) && !Map.IsTile(v, tileDuo.inputTile)) 
                    return tileDuo.outputTile;
        return null;
    }
}