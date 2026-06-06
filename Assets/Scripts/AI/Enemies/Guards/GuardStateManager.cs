using UnityEngine;

namespace Guards
{
    [RequireComponent(typeof(IdleState))]
    [RequireComponent(typeof(PatrollingState))]
    [RequireComponent(typeof(SuspiciousState))]
    [RequireComponent(typeof(ChasingState))]
    [RequireComponent(typeof(SearchingState))]
    [RequireComponent(typeof(GuardNavigator))]
    public class GuardStateManager : MonoBehaviour, ITurnActor
    {
        // Serialized dependencies keep guard behavior data-driven instead of hard-coded.
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private AwarenessManager awarenessManager;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private float detectionRange = 6f;
        [SerializeField] private float fieldOfView = 90f;
        [SerializeField] private ContactFilter2D lineOfSightFilter;
        [SerializeField] private int lineOfSightBufferSize = 4;
        [SerializeField] private Vector2[] patrolPoints;
        [SerializeField] private float suspicionPerTick = 10f;
        [SerializeField] private float maxSuspicion = 100f;
        [SerializeField] private float immediateDetectionRange = 1f;

        // One active state at a time keeps the guard behavior easy to reason about and test.
        BaseState currentState;

        private IdleState idleState;
        private PatrollingState patrollingState;
        private SuspiciousState suspiciousState;
        private ChasingState chasingState;
        private SearchingState searchingState;

        private RaycastHit2D[] hitBuffer;
        private int patrolIndex;

        private const float MinDirectionSqrMagnitude = 0.0001f;

        private bool isPlayerDetected = false;
        private bool detectionCheckedThisTick = false;

        public Vector2 LastKnownPlayerPosition { get; private set; }
        public float Suspicion { get; private set; }
        public float MaxSuspicion => maxSuspicion;
        public float SuspicionRatio => maxSuspicion <= 0f ? 0f : Suspicion / maxSuspicion;

        public Transform PlayerTarget => playerTarget;
        public IdleState IdleState => idleState;
        public PatrollingState PatrollingState => patrollingState;
        public SuspiciousState SuspiciousState => suspiciousState;
        public ChasingState ChasingState => chasingState;
        public SearchingState SearchingState => searchingState;

        public bool IsChasing => currentState == chasingState;

        public GuardNavigator Navigator;
        public int TickDebt { get; set; }

        private void OnEnable()
        {
            idleState = GetComponent<IdleState>();
            patrollingState = GetComponent<PatrollingState>();
            suspiciousState = GetComponent<SuspiciousState>();
            chasingState = GetComponent<ChasingState>();
            searchingState = GetComponent<SearchingState>();
            Navigator = GetComponent<GuardNavigator>();

            hitBuffer = new RaycastHit2D[Mathf.Max(1, lineOfSightBufferSize)];

            // Patrol is the default so guards resume a safe, deterministic baseline.
            currentState = patrollingState;
            currentState.EnterState();

            turnManager.Register(this);

            if (awarenessManager == null)
            {
                awarenessManager = AwarenessManager.Instance;
            }

            awarenessManager?.RegisterGuard(this);
        }

        private void OnDisable()
        {
            turnManager.Unregister(this);
            awarenessManager?.UnregisterGuard(this);
        }

        public async Awaitable OnTick()
        {
            // Tick debt lets the turn system catch up without skipping intermediate guard decisions.
            while (TickDebt > 0)
            {
                awarenessManager.ReportGuardSuspicion(Mathf.Min(suspicionPerTick, Suspicion));
                if (!IsChasing) Suspicion = Mathf.Max(0f, Suspicion - suspicionPerTick);
                currentState.TickState();
                TickDebt--;
            }
            detectionCheckedThisTick = false;
            return;
        }

        public void UpdateState(BaseState newState)
        {
            // Exit/enter hooks centralize state transitions so each state owns its own setup and cleanup.
            currentState.ExitState();
            currentState = newState;
            currentState.EnterState();
        }

        public bool IsPlayerDetected()
        {
            if (playerTarget == null)
            {
                return false;
            }

            if (!detectionCheckedThisTick)
            {
                detectionCheckedThisTick = true;
                Vector2 detectionForward = ResolveDetectionForward(isPlayerDetected);
                isPlayerDetected = DetectionUtils.IsDetected(
                transform.position,
                detectionForward,
                playerTarget,
                detectionRange,
                fieldOfView,
                lineOfSightFilter,
                hitBuffer,
                immediateDetectionRange);
                if (isPlayerDetected)
                {
                    LastKnownPlayerPosition = playerTarget.position;
                    awarenessManager?.ReportPlayerSeen(LastKnownPlayerPosition);
                }
            }
            return isPlayerDetected;
        }

        private Vector2 ResolveDetectionForward(bool preferPlayerFocus)
        {
            if ((preferPlayerFocus || IsChasing) && playerTarget != null)
            {
                Vector2 toPlayer = (Vector2)playerTarget.position - (Vector2)transform.position;
                if (toPlayer.sqrMagnitude > MinDirectionSqrMagnitude)
                {
                    return toPlayer.normalized;
                }
            }

            if (Navigator != null)
            {
                Vector2 moveDirection = Navigator.LastMoveDirection;
                if (moveDirection.sqrMagnitude > MinDirectionSqrMagnitude)
                {
                    return moveDirection;
                }
            }

            return transform.up;
        }

        public void ResetSuspicion()
        {
            Suspicion = 0f;
        }

        public void IncreaseSuspicion()
        {
            // Suspicion is clamped so the state machine can rely on a predictable max threshold.
            Suspicion = Mathf.Min(maxSuspicion, Suspicion + suspicionPerTick);
            if (Suspicion >= maxSuspicion)
            {
                awarenessManager.ReportGuardSuspicion(Suspicion);
                UpdateState(chasingState);
            }
        }

        public Vector2? GetCurrentPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return null;
            }

            patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1);
            return patrolPoints[patrolIndex];
        }

        public void InvestigatePosition(Vector2 position)
        {
            LastKnownPlayerPosition = position;

            if (Navigator != null)
            {
                Navigator.SetDestination(position, true);
            }

            if (currentState != searchingState && currentState != suspiciousState && currentState != chasingState)
            {
                UpdateState(searchingState);
            }
        }

        public Vector2? AdvancePatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return null;
            }

            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            return patrolPoints[patrolIndex];
        }

        private void OnDrawGizmosSelected()
        {
            DrawPatrolPoints();
            DrawFieldOfView();
        }

        private void DrawPatrolPoints()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return;
            }

            Gizmos.color = Color.cyan;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Vector3 point = patrolPoints[i];
                Gizmos.DrawSphere(point, 0.15f);

                if (patrolPoints.Length > 1)
                {
                    Vector3 nextPoint = patrolPoints[(i + 1) % patrolPoints.Length];
                    Gizmos.DrawLine(point, nextPoint);
                }
            }
        }

        private void DrawFieldOfView()
        {
            if (detectionRange <= 0f)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position;
            Vector3 forward = ResolveDetectionForward(isPlayerDetected);

            Gizmos.DrawWireSphere(origin, detectionRange);

            if (fieldOfView <= 0f || fieldOfView >= 360f)
            {
                return;
            }

            float halfFov = fieldOfView * 0.5f;
            Vector3 leftDirection = Quaternion.Euler(0f, 0f, -halfFov) * forward;
            Vector3 rightDirection = Quaternion.Euler(0f, 0f, halfFov) * forward;

            Gizmos.DrawLine(origin, origin + leftDirection * detectionRange);
            Gizmos.DrawLine(origin, origin + rightDirection * detectionRange);
        }

        public void SetBaseSuspicion()
        {
            Suspicion += awarenessManager.awareness;
        }

        public AwarenessLevel AwarenessLevel()
        {
            return awarenessManager.CurrentLevel;
        }
    }
}
