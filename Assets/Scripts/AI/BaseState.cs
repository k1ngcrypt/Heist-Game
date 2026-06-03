using Guards;
using UnityEngine;

public abstract class BaseState : MonoBehaviour
{
    protected GuardStateManager Manager { get; private set; }
    protected GuardIcon _icon;

    protected virtual void Awake()
    {
        Manager = GetComponent<GuardStateManager>();
        _icon = GetComponentInChildren<GuardIcon>();
    }

    // Logic to run when first entering the state
    public abstract void EnterState();

    // Logic to run every tick
    public abstract void TickState();

    // Logic to run when leaving the state
    public abstract void ExitState();
}
