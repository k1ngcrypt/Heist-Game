public abstract class BaseState
{
    // Logic to run when first entering the state
    public abstract void EnterState();

    // Logic to run every tick
    public abstract void TickState();

    // Logic to run when leaving the state
    public abstract void ExitState();
}