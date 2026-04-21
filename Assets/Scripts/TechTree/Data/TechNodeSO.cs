using UnityEngine;
using System.Collections.Generic;

public enum PrerequisiteMode
{
    All,
    Any
}

[CreateAssetMenu(fileName = "New Tech Node", menuName = "Tech Tree/Tech Node")]
public class TechNodeSO : ScriptableObject
{
    [Header("Basic Info")]
    public string techID;
    public string techName;
    [TextArea(2, 5)] public string description;
    public Sprite techIcon;
    [Min(0)] public int tier;
    public string category;
    public int resourceCost;

    [Header("Requirements")]
    public PrerequisiteMode prerequisiteMode = PrerequisiteMode.All;
    public List<TechNodeSO> prerequisites = new();

    [Header("Designer Layout")]
    public Vector2 editorPosition;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(techID))
        {
            techID = name;
        }

        if (string.IsNullOrWhiteSpace(techName))
        {
            techName = name;
        }

        if (resourceCost < 0)
        {
            resourceCost = 0;
        }

        if (tier < 0)
        {
            tier = 0;
        }

        if (prerequisites == null)
        {
            prerequisites = new List<TechNodeSO>();
        }
    }
}
