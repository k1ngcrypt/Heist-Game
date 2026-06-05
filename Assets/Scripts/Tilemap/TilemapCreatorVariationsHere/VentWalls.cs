using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class VentWalls : DuplicateTilemapBase
{
    [Header("Tile References")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private RuleTile floorTile;
    [SerializeField] private RuleTile wallTile;
    [SerializeField] private GameObject wallTop;

    protected override bool OnCreation() {
        if (floorTilemap==null||floorTile==null||wallTile==null) return false;
        floorTilemap.CompressBounds();
        var tBounds = floorTilemap.cellBounds;
        int xMin = tBounds.xMin, xMax = tBounds.xMax, yMin = tBounds.yMin, yMax = tBounds.yMax;
        bounds = new();
        bool b = true;
        for (int x = xMin; x<=xMax; x++) for (int y = yMin; y<=xMax; y++) {
                if (floorTilemap.GetTile(new Vector3Int(x,y,0))==floorTile) {
                    if (b) {
                        b=false;
                        bounds.xMin = x;
                        bounds.xMax = x;
                        bounds.yMin = y;
                        bounds.yMax = y; 
                    }
                    bounds.xMin = Mathf.Min(bounds.xMin, x);
                    bounds.xMax = Mathf.Max(bounds.xMax, x);
                    bounds.yMin = Mathf.Min(bounds.yMin, y);
                    bounds.yMax = Mathf.Max(bounds.yMax, y);
                }
        }
        bounds.xMin-=1;
        bounds.yMin-=1;
        bounds.xMax+=1;
        bounds.yMax+=1;
        if (wallTop==null) return true;
        wallTop.transform.position = bounds.center;
        wallTop.transform.localScale = bounds.size+Vector3.one*21;
        return true;
    }
    
    protected override TileBase GetTile(Vector3Int v) {
        if (floorTilemap.GetTile(v)==floorTile) return null;
        return wallTile;
    }
}
