using UnityEngine;

public interface ITurnActor
{
    int TickDebt { get; set; }    // Ticks "owed" before they act again

    Awaitable OnTick();           // Called once per global tick
}
