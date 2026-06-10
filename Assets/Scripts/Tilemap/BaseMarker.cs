using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class BaseMarker : MonoBehaviour
{
    private void OnValidate()
    {
        // Get required component
        Tilemap _tilemap = GetComponent<Tilemap>();

        if (_tilemap == null) {
            Debug.LogError("[BaseMarker] No Tilemap component found on this GameObject!", this);
            return;
        }

        // Initialize the Map system
        Map.SetBase(_tilemap);

        Debug.Log($"[BaseMarker] Map system initialized with tilemap: {gameObject.name}", this);
    }

    private void Awake()
    {
        // Get required component
        Tilemap _tilemap = GetComponent<Tilemap>();

        if (_tilemap == null) {
            Debug.LogError("[BaseMarker] No Tilemap component found on this GameObject!", this);
            return;
        }

        // Initialize the Map system
        Map.SetBase(_tilemap);

        Debug.Log($"[BaseMarker] Map system initialized with tilemap: {gameObject.name}", this);
    }
}