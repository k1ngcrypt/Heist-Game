using UnityEngine;
using System.Collections.Generic;

public enum ItemType
{
    
    Armour,
    Weapon,
    Gadget    
}

[CreateAssetMenu(fileName = "LoadoutItems", menuName = "Scriptable Objects/LoadoutItems")]
public class LoadoutItems : ScriptableObject
{
    [Header("Text")]
    public ItemType itemType;
    public string itemTitle;
    [TextArea(2, 5)] public string itemDescription;
    public Sprite itemIcon;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemTitle))
        {
            itemTitle = name;
        }
    }
}
