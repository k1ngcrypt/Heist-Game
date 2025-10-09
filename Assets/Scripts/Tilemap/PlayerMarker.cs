using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerMarker : MonoBehaviour
{
    void Awake()
    {
        Map.SetPlayerTilemap(gameObject.GetComponent<Tilemap>());
    }
}
