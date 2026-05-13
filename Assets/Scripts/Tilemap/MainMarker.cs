using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class MainMarker : MonoBehaviour
{
    private Tilemap _tilemap;
    private void OnValidate()
    {
        // Get required component
        _tilemap = GetComponent<Tilemap>();

        if (_tilemap == null)
        {
            Debug.LogError("[MainMarker] No Tilemap component found on this GameObject!", this);
            return;
        }

        // Initialize the Map system
        Map.SetWall(_tilemap);

        Debug.Log($"[MainMarker] Map system initialized with tilemap: {gameObject.name}", this);
    }
}