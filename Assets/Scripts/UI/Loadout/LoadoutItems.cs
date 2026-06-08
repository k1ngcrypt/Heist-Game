using UnityEngine;

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
    [SerializeField] public ItemType itemType;
    [SerializeField] public string itemTitle;
    [TextArea(2, 5)] [SerializeField] public string itemDescription;
    [SerializeField] public Sprite itemIcon;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemTitle))
        {
            itemTitle = name;
        }
    }
}
