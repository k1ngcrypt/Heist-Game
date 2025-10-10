using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New WithNullTile", menuName = "Tiles/WithNullTile")]
public class WithNullTile : RuleTile 
{
    [Header("Custom Settings")]
    [Tooltip("Custom field for additional tile behavior")]
    public bool customField;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    /// <summary>
    /// Enhanced rule matching that considers the Map system's null tile.
    /// Provides better integration with the overall tilemap system.
    /// </summary>
    /// <param name="neighbor">The neighbor rule to check</param>
    /// <param name="other">The tile to compare against</param>
    /// <returns>True if the rule matches</returns>
    public override bool RuleMatch(int neighbor, TileBase other) 
    {
        // Handle RuleOverrideTile unwrapping
        if (other is RuleOverrideTile overrideTile)
        {
            other = overrideTile.m_InstanceTile;
        }

        // Get null tile reference from Map system if available
        RuleTile nullTileReference = Map.IsInitialized ? Map.NullTile : null;

        if (showDebugLogs && nullTileReference == null)
        {
            Debug.LogWarning($"[WithNullTile] Map system not initialized or null tile not available for {name}", this);
        }

        switch (neighbor)
        {
            case TilingRuleOutput.Neighbor.This: 
                // Consider both this tile and the null tile as "This"
                bool matchesThis = other == this;
                bool matchesNull = nullTileReference != null && other == nullTileReference;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[WithNullTile] This rule - other: {other?.name ?? "null"}, matches this: {matchesThis}, matches null: {matchesNull}", this);
                }
                
                return matchesThis || matchesNull;
                
            case TilingRuleOutput.Neighbor.NotThis: 
                // Consider anything that's not this tile and not the null tile as "NotThis"
                bool doesNotMatchThis = other != this;
                bool doesNotMatchNull = nullTileReference == null || other != nullTileReference;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[WithNullTile] NotThis rule - other: {other?.name ?? "null"}, doesn't match this: {doesNotMatchThis}, doesn't match null: {doesNotMatchNull}", this);
                }
                
                return doesNotMatchThis && doesNotMatchNull;
        }

        // For all other neighbor types, use default behavior
        return base.RuleMatch(neighbor, other);
    }

    /// <summary>
    /// Enhanced GetTileData method that provides additional debug information.
    /// </summary>
    /// <param name="position">The position of the tile</param>
    /// <param name="tilemap">The tilemap reference</param>
    /// <param name="tileData">The tile data to populate</param>
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);
        
        if (showDebugLogs)
        {
            Debug.Log($"[WithNullTile] GetTileData called for {name} at position {position}", this);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor validation to ensure proper setup.
    /// </summary>
    private void OnValidate()
    {
        // Ensure the tile has a proper name for debugging
        if (string.IsNullOrEmpty(name))
        {
            name = "WithNullTile";
        }
    }
#endif
}