using System.Collections.Generic;
using UnityEngine;

// TurnManager.cs
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private readonly List<ITurnActor> _actors = new();
    private readonly List<ITurnActor> _pendingActors = new();
    private readonly List<ITurnActor> _pendingRemovals = new();
    private bool _isProcessing = false;

    void Awake() => Instance = this;

    public void Register(ITurnActor actor)
    {
        if (_isProcessing)
        {
            if (!_pendingActors.Contains(actor) && !_actors.Contains(actor))
            {
                _pendingActors.Add(actor);
            }

            _pendingRemovals.Remove(actor);

            return;
        }

        if (!_actors.Contains(actor))
        {
            _actors.Add(actor);
        }
    }
    public void Unregister(ITurnActor actor)
    {
        if (_isProcessing)
        {
            if (!_pendingRemovals.Contains(actor))
            {
                _pendingRemovals.Add(actor);
            }

            _pendingActors.Remove(actor);
            return;
        }

        _actors.Remove(actor);
    }

    // Called once the player submits an action
    public async Awaitable ProcessTicks(int ticksToAdvance)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        try
        {
            for (int t = 0; t < ticksToAdvance; t++)
            {
                ApplyPendingChanges();
                var tickActors = _actors.ToArray();
                foreach (var actor in tickActors)
                {
                    if (actor == null)
                    {
                        if (!_pendingRemovals.Contains(actor))
                        {
                            _pendingRemovals.Add(actor);
                        }

                        continue;
                    }

                    if (_pendingRemovals.Contains(actor))
                    {
                        continue;
                    }
                    actor.TickDebt++;
                    if (actor.TickDebt > 0)
                    {
                        //Debug.Log($"Processing tick for {actor}. Tick debt: {actor.TickDebt}");
                        await actor.OnTick(); // Do not forget to decrement tick debt accordingly.
                    }
                    
                }

            }

            ApplyPendingChanges();
        }
        finally { _isProcessing = false; }
        return;
    }

    private void ApplyPendingChanges()
    {
        if (_pendingRemovals.Count > 0)
        {
            foreach (var actor in _pendingRemovals)
            {
                _actors.Remove(actor);
            }

            _pendingRemovals.Clear();
        }

        if (_pendingActors.Count > 0)
        {
            _actors.AddRange(_pendingActors);
            _pendingActors.Clear();
        }
    }
}