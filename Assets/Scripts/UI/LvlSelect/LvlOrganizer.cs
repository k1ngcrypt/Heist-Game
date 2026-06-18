using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "LvlOrganizer", menuName = "Heist Game/LvlOrganizer")]
public class LvlOrganizer : ScriptableObject
{
    [Header("Scene To Be Called")]
#if UNITY_EDITOR
    [SerializeField] public SceneAsset sceneAsset;
#endif
    [SerializeField] public string sceneName;

    [Header("Text")]
    [SerializeField] public string lvlTitle;
    [TextArea(2, 5)][SerializeField] public string lvlDescription;
    [SerializeField] public Sprite lvlIcon;
    [SerializeField] public List<string> objectiveText = new();

    [Header("Requirements")]
    [SerializeField] public List<LvlOrganizer> prerequisites = new();

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(lvlTitle))
        {
            lvlTitle = name;
        }
#if UNITY_EDITOR
        if (sceneAsset != null)
        {
            sceneName = sceneAsset.name;
        }
#endif
        objectiveText ??= new List<string>();
        prerequisites ??= new List<LvlOrganizer>();
    }
}
