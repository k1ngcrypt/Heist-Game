using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public abstract class DuplicateTilemapBase : MonoBehaviour
{
    protected Tilemap _thisTilemap;
    protected BoundsInt bounds = new();
    private bool queued = false;

    void OnValidate()
    {
        _thisTilemap = GetComponent<Tilemap>();
        if (_thisTilemap == null||queued) return;
        if (!OnCreation()) return;
        queued = true;
        EditorApplication.delayCall += () => {
            if (this==null||Application.isPlaying) return; //Yes, this is intetional, please, do not flag it.
            queued = false;
            if (Map.IsInitialized&&bounds == default)bounds = Map.Bounds;
            var positions = new System.Collections.Generic.List<Vector3Int>();
            var tiles = new System.Collections.Generic.List<TileBase>();

            for (int x = bounds.xMin; x <= bounds.xMax; x++)
                for (int y = bounds.yMin; y <= bounds.yMax + 1; y++) {
                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(GetTile(new Vector3Int(x, y, 0)));
                }
            _thisTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            Debug.Log("["+GetType().Name+"] Generated "+positions.Count+" tile"+(positions.Count==1?"":"s")+" in tilemap "+gameObject.name);
        };
    }

    protected abstract bool OnCreation();

    protected abstract TileBase GetTile(Vector3Int v);
}
