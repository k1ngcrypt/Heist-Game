using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapTop : MonoBehaviour
{
    public RuleTile WallTile, TopTile, CornerTile;
    private Tilemap thisTilemap;
    void Start()
    {
        thisTilemap = gameObject.GetComponent<Tilemap>();
        for (int x = Map.xMin; x < Map.xMax; x++)
            for (int y = Map.yMin; y < Map.yMax+1; y++)
                if (Map.IsTile(x, y-1, WallTile) && !Map.IsTile(x, y, WallTile))
                    thisTilemap.SetTile(new Vector3Int(x, y, 0), TopTile);
                else if (Map.IsTile(x, y, WallTile)&&((Map.IsTile(x+1, y-1, WallTile) && !Map.IsTile(x+1, y, WallTile))||(Map.IsTile(x-1, y-1, WallTile) && !Map.IsTile(x-1, y, WallTile))))
                    thisTilemap.SetTile(new Vector3Int(x, y, 0), CornerTile);
                else thisTilemap.SetTile(new Vector3Int(x, y, 0), null);
    }
}
