using UnityEngine;

namespace Guards
{
    public class GuardStateManager : MonoBehaviour, ITurnActor
    {
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private GuardNavigator navigator;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private float detectionRange = 6f;
        [SerializeField] private float fieldOfView = 90f;
        [SerializeField] private ContactFilter2D lineOfSightFilter;
        [SerializeField] private int lineOfSightBufferSize = 4;
        [SerializeField] private Transform[] patrolPoints;

        BaseState currentState;

        private IdleState idleState;
        private PatrollingState patrollingState;
        private SuspiciousState suspiciousState;
        private ChasingState chasingState;
        private SearchingState searchingState;

        private RaycastHit2D[] hitBuffer;
        private int patrolIndex;

        public Vector2 LastKnownPlayerPosition { get; private set; }

        public GuardNavigator Navigator => navigator;
        public Transform PlayerTarget => playerTarget;
        public IdleState IdleState => idleState;
        public PatrollingState PatrollingState => patrollingState;
        public SuspiciousState SuspiciousState => suspiciousState;
        public ChasingState ChasingState => chasingState;
        public SearchingState SearchingState => searchingState;

        public int TickDebt { get; set; }

        private void OnEnable()
        {
            idleState = GetComponent<IdleState>();
            patrollingState = GetComponent<PatrollingState>();
            suspiciousState = GetComponent<SuspiciousState>();
            chasingState = GetComponent<ChasingState>();
            searchingState = GetComponent<SearchingState>();
            navigator = navigator == null ? GetComponent<GuardNavigator>() : navigator;

            hitBuffer = new RaycastHit2D[Mathf.Max(1, lineOfSightBufferSize)];

            currentState = patrollingState;
            currentState.EnterState();

            turnManager.Register(this);
        }

        private void OnDisable()
        {
            turnManager.Unregister(this);
        }

        public Awaitable OnTick()
        {
            currentState.TickState();
            if (TickDebt > 0)
            {
                TickDebt--;
            }

            return default;
        }

        public void UpdateState(BaseState newState)
        {
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

            return DetectionUtils.IsDetected(
                transform.position,
                transform.up,
                playerTarget,
                detectionRange,
                fieldOfView,
                lineOfSightFilter,
                hitBuffer);
        }

        public void UpdateLastKnownPlayerPosition()
        {
            if (playerTarget == null)
            {
                return;
            }

            LastKnownPlayerPosition = playerTarget.position;
        }

        public Transform GetCurrentPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return null;
            }

            patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1);
            return patrolPoints[patrolIndex];
        }

        public Transform AdvancePatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return null;
            }

            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            return patrolPoints[patrolIndex];
        }

    }
}
