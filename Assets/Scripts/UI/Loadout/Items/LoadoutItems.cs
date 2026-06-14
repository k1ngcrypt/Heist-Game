using UnityEngine;

public abstract class LoadoutItems : ScriptableObject {
    [Header("General Item Stuff")]
    
    [SerializeField] public string itemTitle;
    [TextArea(2, 5)] [SerializeField] public string itemDescription;
    [SerializeField] public Sprite itemIcon;
    [SerializeField] public float suspicionModifier;
}
