using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "LvlOrganizer", menuName = "Scriptable Objects/LvlOrganizer")]
public class LvlOrganizer : ScriptableObject
{
    [Header("Scene To Be Called")]
    #if UNITY_EDITOR
    public SceneAsset sceneAsset;
    #endif
    public string sceneName;

    [Header("Text")]
    public string lvlTitle;
    [TextArea(2, 5)] public string lvlDescription;
    public Sprite lvlIcon;

    [Header("Requirements")]
    public List<LvlOrganizer> prerequisites = new();
    
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(lvlTitle))
        {
            lvlTitle = name;
        }

        if (sceneAsset != null)
        {
            sceneName = sceneAsset.name;
        }

        if (prerequisites == null)
        {
            prerequisites = new List<LvlOrganizer>();
        }
    }
}
