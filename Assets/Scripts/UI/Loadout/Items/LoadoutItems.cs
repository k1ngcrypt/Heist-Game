using UnityEngine;

[CreateAssetMenu(fileName = "GadgetItem", menuName = "Heist Game/Loadout Items/Item")]
public class LoadoutItems : ScriptableObject {
    [Header("General Item Stuff")]
    
    [SerializeField] public int itemID;
    [SerializeField] public string itemTitle;
    [TextArea(2, 5)] [SerializeField] public string itemDescription;
    [SerializeField] public Sprite itemIcon;
}
