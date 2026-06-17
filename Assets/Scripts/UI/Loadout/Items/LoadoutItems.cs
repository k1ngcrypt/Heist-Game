using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class LoadoutItems : ScriptableObject {
    [Header("General Item Stuff")]
    
    [SerializeField] public string itemTitle;
    [TextArea(2, 5)] [SerializeField] public string itemDescription;
    [SerializeField] public Sprite itemIcon;
    [SerializeField] public float suspicionModifier;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]

    public abstract void ResetItemStats();
}
