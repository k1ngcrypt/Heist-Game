using System.Collections.Generic;
using UnityEngine;

// TurnManager.cs
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private readonly List<ITurnActor> _actors = new();
    private bool _isProcessing = false;

    void Awake() => Instance = this;

    public void Register(ITurnActor actor) => _actors.Add(actor);
    public void Unregister(ITurnActor actor) => _actors.Remove(actor);

    // Called once the player submits an action
    public async Awaitable ProcessTicks(int ticksToAdvance)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        try
        {
            for (int t = 0; t < ticksToAdvance; t++)
            {
                foreach (var actor in _actors)
                {
                    actor.TickDebt++;
                    if (actor.TickDebt > 0)
                    {
                        await actor.OnTick();//Do not forget to decrement tick debt accordingly.
                    }
                    
                }
            }
        }
        finally { _isProcessing = false; }
    }
}