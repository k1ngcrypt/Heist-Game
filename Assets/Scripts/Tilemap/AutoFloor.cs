using UnityEngine;
using UnityEngine.Tilemaps;

public class AutoFloor : MonoBehaviour
{
    public RuleTile FloorTile;
    private Tilemap thisTilemap;
    void Start()
    {
        thisTilemap = gameObject.GetComponent<Tilemap>();
        for (int x = Map.xMin; x < Map.xMax; x++)
            for (int y = Map.yMin; y < Map.yMax; y++)
                thisTilemap.SetTile(new Vector3Int(x, y, 0), FloorTile);
    }
}
