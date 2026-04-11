using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Tech Node", menuName = "Tech Tree/Tech Node")]
public class TechNodeSO : ScriptableObject
{
    [Header("Basic Info")]
    public string techID;
    public string techName;
    public Sprite techIcon;
    public int resourceCost;

    [Header("Requirements")]
    public List<TechNodeSO> prerequisites = new();

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(techID))
        {
            techID = name;
        }
    }
}
