using UnityEngine;
using UnityEngine.Tilemaps;

public class MainMarker : MonoBehaviour
{
    void Awake()
    {
        Map.SetTilemap(gameObject.GetComponent<Tilemap>());
    }
}
