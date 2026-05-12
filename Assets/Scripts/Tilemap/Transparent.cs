using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class Transparent : MonoBehaviour
{
    private Tilemap _tilemap;
    private void OnValidate()
    {
        // Get required component
        _tilemap = GetComponent<Tilemap>();

        if (_tilemap == null)
        {
            Debug.LogError("[Transparent] No Tilemap component found on this GameObject!", this);
            return;
        }

        // Initialize the Map system
        Map.SetTransparent(_tilemap);

        Debug.Log($"[Transparent] Map system initialized with tilemap: {gameObject.name}", this);
    }
}