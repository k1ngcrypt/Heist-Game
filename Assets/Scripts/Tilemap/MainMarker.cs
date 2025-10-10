using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Initializes the Map system with this GameObject's Tilemap component.
/// Should be attached to the main tilemap GameObject.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class MainMarker : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private RuleTile nullTile; // assign in inspector instead of using Resources
    [SerializeField] private bool refreshBoundsOnStart = true;

    private Tilemap _tilemap;

    /// <summary>
    /// Initialize components as early as possible.
    /// </summary>
    private void Awake()
    {
        // Get required component
        _tilemap = GetComponent<Tilemap>();

        if (_tilemap == null)
        {
            Debug.LogError("[MainMarker] No Tilemap component found on this GameObject!", this);
            return;
        }

        // Initialize the Map system
        Map.SetTilemap(_tilemap, nullTile);

        Debug.Log($"[MainMarker] Map system initialized with tilemap: {gameObject.name}", this);
    }

    /// <summary>
    /// Refresh bounds if needed after all initialization is complete.
    /// </summary>
    private void Start()
    {
        if (refreshBoundsOnStart && Map.IsInitialized)
        {
            Map.RefreshBounds();
            Debug.Log($"[MainMarker] Bounds refreshed: {Map.Bounds}", this);
        }
    }

    /// <summary>
    /// Ensure Map system is properly cleaned up.
    /// </summary>
    private void OnDestroy()
    {
        // Clear cache when this tilemap is destroyed
        if (Map.IsInitialized && Map.Tilemap == _tilemap)
        {
            Map.ClearCache();
            Debug.Log("[MainMarker] Map cache cleared on destroy", this);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Validate setup in the editor.
    /// </summary>
    private void OnValidate()
    {
        if (_tilemap == null)
            _tilemap = GetComponent<Tilemap>();

        // Check if null tile resource is assigned
        if (nullTile == null)
            Debug.LogWarning("[MainMarker] Null tile not assigned!", this);
    }

    /// <summary>
    /// Editor context menu to manually refresh the Map system.
    /// </summary>
    [ContextMenu("Refresh Map System")]
    private void RefreshMapSystem()
    {
        if (_tilemap != null)
        {
            Map.SetTilemap(_tilemap, nullTile);
            Debug.Log("[MainMarker] Map system manually refreshed", this);
        }
    }
#endif
}
