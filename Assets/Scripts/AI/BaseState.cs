using Guards;
using UnityEngine;

public abstract class BaseState : MonoBehaviour
{
    protected GuardStateManager Manager { get; private set; }

    protected virtual void Awake()
    {
        Manager = GetComponent<GuardStateManager>();
    }

    // Logic to run when first entering the state
    public abstract void EnterState();

    // Logic to run every tick
    public abstract void TickState();

    // Logic to run when leaving the state
    public abstract void ExitState();
}
