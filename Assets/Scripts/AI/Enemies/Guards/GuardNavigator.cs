using System.Collections.Generic;
using UnityEngine;

namespace Guards
{
    public class GuardNavigator : MonoBehaviour
    {
        [SerializeField] private Pathfinder pathfinder;
        [SerializeField] private float arrivalThreshold = 0.05f;
        [SerializeField] private bool repathOnBlocked = true;

        private readonly List<int> currentPath = new();
        private int pathIndex;
        private Vector2 destination;
        private bool hasDestination;

        private void Awake()
        {
            if (pathfinder == null)
            {
                pathfinder = FindObjectOfType<Pathfinder>();
            }
        }

        public bool HasDestination => hasDestination;

        public bool HasPath => currentPath.Count > 0 && pathIndex < currentPath.Count;

        public bool ReachedDestination
        {
            get
            {
                if (!hasDestination)
                {
                    return true;
                }

                return Vector2.Distance(transform.position, destination) <= arrivalThreshold && !HasPath;
            }
        }

        public void SetDestination(Vector2 target, bool forceRepath = false)
        {
            if (!forceRepath && hasDestination && Vector2.Distance(destination, target) <= arrivalThreshold)
            {
                return;
            }

            destination = target;
            hasDestination = true;
            RecalculatePath();
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
