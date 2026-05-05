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

        for (int t = 0; t < ticksToAdvance; t++)
        {
            foreach (var actor in _actors)
            {
                actor.TickDebt++;
                await actor.OnTick();
            }
        }

        _isProcessing = false;
    }
}