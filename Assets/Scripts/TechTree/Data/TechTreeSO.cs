using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Tech Tree", menuName = "Tech Tree/Tech Tree")]
public class TechTreeSO : ScriptableObject
{
    [Header("Tech Tree")]
    public List<TechNodeSO> allTechNodes = new();
    public List<TechNodeSO> startingUnlockedTechs = new();
}
