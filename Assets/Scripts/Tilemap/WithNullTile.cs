using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class WithNullTile : RuleTile {
    public bool customField;

    public override bool RuleMatch(int neighbor, TileBase other) {
        if (other is RuleOverrideTile ot)
            other = ot.m_InstanceTile;

        switch (neighbor)
        {
            case TilingRuleOutput.Neighbor.This: return other == this || other == Map.nullTile;
            case TilingRuleOutput.Neighbor.NotThis: return other != this && other != Map.nullTile;
        }

        return true;
    }
}