using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class LoadoutItems : ScriptableObject {
    [Header("General Item Stuff")]
    
    [SerializeField] public string itemTitle;
    [TextArea(2, 5)] [SerializeField] public string itemDescription;
    [SerializeField] public Sprite itemIcon;
    [SerializeField] public float suspicionModifier;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeSceneListener() { 
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        LoadoutItems[] items = Resources.FindObjectsOfTypeAll<LoadoutItems>();

        foreach (LoadoutItems item in items) item.ResetItemStats();
    }

    protected abstract void ResetItemStats();
}
