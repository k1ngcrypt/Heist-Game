using UnityEngine;

namespace Guards
{
    public class GuardStateManager : MonoBehaviour, ITurnActor
    {
        [SerializeField] private TurnManager turnManager;

        BaseState currentState;

        private IdleState idleState;
        private PatrollingState patrollingState;
        private SuspiciousState suspiciousState;
        private ChasingState chasingState;
        private SearchingState searchingState;

        public int TickDebt { get; set; }

        private void OnEnable()
        {
            idleState = GetComponent<IdleState>();
            patrollingState = GetComponent<PatrollingState>();
            suspiciousState = GetComponent<SuspiciousState>();
            chasingState = GetComponent<ChasingState>();
            searchingState = GetComponent<SearchingState>();

            currentState = patrollingState;

            turnManager.Register(this);
        }

        private void OnDisable()
        {
            turnManager.Unregister(this);
        }

        public Awaitable OnTick()
        {
            currentState.TickState();
            return default;
        }

        public void UpdateState(BaseState newState)
        {
            currentState.ExitState();
            currentState = newState;
            currentState.EnterState();
        }

    }
}
