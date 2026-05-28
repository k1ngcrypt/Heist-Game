using System.Collections.Generic;
using UnityEngine;

namespace Guards
{
    public class GuardNavigator : MonoBehaviour
    {
        // Pathfinding is injected so navigation can be swapped or reused without rewriting movement logic.
        [SerializeField] private Pathfinder pathfinder;
        [SerializeField] private float arrivalThreshold = 0.05f;
        [SerializeField] private bool repathOnBlocked = true;

        private readonly List<int> currentPath = new();
        private int pathIndex;
        private Vector2 destination;
        private bool hasDestination;
        private Vector2 lastMoveDirection;

        public bool HasDestination => hasDestination;
        public Vector2 LastMoveDirection => lastMoveDirection;

        // A path only matters while there are still unvisited nodes left to consume.
        public bool HasPath => currentPath.Count > 0 && pathIndex < currentPath.Count;

        public bool ReachedDestination
        {
            get
            {
                // Arrival uses distance plus path exhaustion so the guard does not stop early on a nearby waypoint.
                return !hasDestination || (Vector2.Distance(transform.position, destination) <= arrivalThreshold && !HasPath);
            }
        }

        public void SetDestination(Vector2 target, bool forceRepath = false)
        {
            // Ignore tiny destination changes to avoid unnecessary path rebuilds every tick.
            if (!forceRepath && hasDestination && Vector2.Distance(destination, target) <= arrivalThreshold)
            {
                return;
            }

            destination = target;
            hasDestination = true;
            RecalculatePath();
        }

        private void Awake()
        {
            lastMoveDirection = transform.up;
        }

        public void ClearDestination()
        {
            hasDestination = false;
            currentPath.Clear();
            pathIndex = 0;
        }

        public void TickAdvance()
        {
            if (!hasDestination || pathfinder == null)
            {
                return;
            }

            // Recompute lazily so navigation cost is paid only when the guard actually needs to move.
            if (!HasPath)
            {
                RecalculatePath();
            }

            if (!HasPath)
            {
                return;
            }

            SkipReachedNodes();
            if (!HasPath)
            {
                return;
            }

            int nextIndex = currentPath[pathIndex];
            if (!TryHandleInteraction(nextIndex))
            {
                if (repathOnBlocked)
                {
                    RecalculatePath();
                }

                return;
            }

            Vector2 nextPosition = pathfinder.GetNodeWorldPosition(nextIndex);
            Vector2 delta = nextPosition - (Vector2)transform.position;
            if (delta.sqrMagnitude > 0.0001f)
            {
                lastMoveDirection = delta.normalized;
            }
            transform.position = nextPosition;
            pathIndex++;
        }

        private void RecalculatePath()
        {
            currentPath.Clear();
            pathIndex = 0;
            if (!hasDestination || pathfinder == null)
            {
                return;
            }

            // The pathfinder returns node indices so movement can follow the same grid used for collision checks.
            List<int> newPath = pathfinder.FindPathIndices(transform.position, destination);
            if (newPath.Count == 0)
            {
                return;
            }

            currentPath.AddRange(newPath);
            SkipReachedNodes();
        }

        private void SkipReachedNodes()
        {
            // Skip already-reached nodes so the guard does not oscillate around its current tile.
            while (pathIndex < currentPath.Count)
            {
                Vector2 nodePosition = pathfinder.GetNodeWorldPosition(currentPath[pathIndex]);
                if (Vector2.Distance(transform.position, nodePosition) <= arrivalThreshold)
                {
                    pathIndex++;
                    continue;
                }

                break;
            }
        }

        private bool TryHandleInteraction(int nodeIndex)
        {
            if (!pathfinder.TryGetNodeInteraction(nodeIndex, out ISpecialTile interaction))
            {
                return true;
            }

            // Interactions are allowed to block movement until they report that passage is safe.
            if (!interaction.CanPass())
            {
                interaction.OnPass();
                return interaction.CanPass();
            }

            interaction.OnPass();
            return true;
        }
    }
}
