using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class WithOtherTile : RuleTile {
    public bool customField;

    public override bool RuleMatch(int neighbor, TileBase other) {
        if (other is RuleOverrideTile ot)
            other = ot.m_InstanceTile;

        switch (neighbor)
        {
            case TilingRuleOutput.Neighbor.This: return other != null;
            case TilingRuleOutput.Neighbor.NotThis: return other == null;
        }

        return true;
    }
}