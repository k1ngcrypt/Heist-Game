using UnityEngine;

public class ShadowBack : MonoBehaviour
{
    void OnValidate()
    {
        if (!Map.IsInitialized) return;
        transform.position = new Vector3(Map.Bounds.center.x, Map.Bounds.center.y, 0);
        transform.localScale = new Vector3(Map.Bounds.size.x+2, Map.Bounds.size.y+2, 1);
    }
}
