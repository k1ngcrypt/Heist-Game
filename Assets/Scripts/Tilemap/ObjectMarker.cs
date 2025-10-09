using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectMarker : MonoBehaviour
{
    void Awake()
    {
        Map.SetTilemap(gameObject.GetComponent<Tilemap>());
    }
}
