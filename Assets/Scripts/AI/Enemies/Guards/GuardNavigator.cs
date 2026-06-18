using System.Collections.Generic;
using UnityEngine;

namespace Guards
{
    [RequireComponent(typeof(Animator))]
    public class GuardNavigator : MonoBehaviour
    {
        // Pathfinding is injected so navigation can be swapped or reused without rewriting movement logic.
        [SerializeField] private Pathfinder pathfinder;
        [SerializeField] private float arrivalThreshold = 0.05f;
        [SerializeField] private bool repathOnBlocked = true;

        private readonly List<int> currentPath = new();
        private int pathIndex;
        private Vector2 destination;
        private Vector2 nextPosition;
        private const float GUARDSPEED = 15f;
        private readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private readonly int DirectionHash = Animator.StringToHash("Direction");
        private Animator animator;
        private bool hasDestination;
        private Vector2 lastMoveDirection;

        public bool HasDestination => hasDestination;
        public Vector2 LastMoveDirection => lastMoveDirection;

        // A path only matters while there are still unvisited nodes left to consume.
        public bool HasPath => currentPath.Count > 0 && pathIndex < currentPath.Count;

        private readonly Queue<(int scheduledPathIndex, ISpecialTile interaction)> pendingClears = new();

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
            nextPosition = transform.position;
            animator = GetComponent<Animator>();
        }

        public void ClearDestination()
        {
            hasDestination = false;
            currentPath.Clear();
            pathIndex = 0;
        }

        public void TickAdvance()
        {
            transform.position = nextPosition;
            if (!hasDestination || pathfinder == null) return;

            if (!HasPath)
            {
                RecalculatePath();
            }

            if (!HasPath) return;

            SkipReachedNodes();
            if (!HasPath) return;

            int nextIndex = currentPath[pathIndex];

            // Interaction check is now a one-time gate: only fires when
            // the guard is about to step onto the node, not every tick.
            if (!TryHandleInteraction(nextIndex))
            {
                if (repathOnBlocked) RecalculatePath();
                return;
            }

            nextPosition = pathfinder.GetNodeWorldPosition(nextIndex);
            animator.SetBool(IsWalkingHash, true);
            Vector2 delta = nextPosition - (Vector2)transform.position;
            if (delta.sqrMagnitude > 0.0001f)
            {
                lastMoveDirection = delta.normalized;
                SetAnimationDirection();
            }

            pathIndex++;

            while (pendingClears.Count > 0)
            {
                var (scheduledPathIndex, interaction) = pendingClears.Peek();
                if (pathIndex > scheduledPathIndex)
                {
                    pendingClears.Dequeue();
                    interaction.OnClear();
                }
                else
                {
                    break;
                }
            }
        }

        private void RecalculatePath()
        {
            currentPath.Clear();
            pathIndex = 0;
            pendingClears.Clear();
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

            if (interaction.IsDoor())
            {
                interaction.OnApproach();  // opens the door
                ScheduleClear(interaction);
                return true;              // doors are always passable
            }

            if (!interaction.CanPass())
            {
                return interaction.CanPass();
            }

            return true;
        }

        private void ScheduleClear(ISpecialTile interaction)
        {
            pendingClears.Enqueue((pathIndex+1, interaction));
        }

        public void Update() {
            if (nextPosition != (Vector2)transform.position) transform.position = Vector3.MoveTowards(transform.position, nextPosition, GUARDSPEED * Time.deltaTime);
            else animator.SetBool(IsWalkingHash, false);
        }

        private void SetAnimationDirection() {
            int baseDir = Mathf.RoundToInt(2*(Mathf.Atan2(lastMoveDirection.y, lastMoveDirection.x)/Mathf.PI+1)) % 4;
            if (baseDir == 3) animator.SetInteger(DirectionHash, 1);
            else if (baseDir == 1) animator.SetInteger(DirectionHash, 3);
            else animator.SetInteger(DirectionHash, baseDir);
        }
    }
}
