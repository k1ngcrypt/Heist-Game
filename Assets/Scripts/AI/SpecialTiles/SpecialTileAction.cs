using UnityEngine;
using UnityEngine.Events;

public class SpecialTileAction : MonoBehaviour, ISpecialTile
{
    [SerializeField] private bool allowPass = true;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private UnityEvent onPass;

    private bool hasTriggered;

    public bool CanPass() => allowPass;

    public void OnPass()
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        hasTriggered = true;
        onPass?.Invoke();
    }
}
