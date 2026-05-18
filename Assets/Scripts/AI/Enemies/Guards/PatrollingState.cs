using UnityEngine;

namespace Guards
{
    public class PatrollingState : BaseState
    {
        public override void EnterState()
        {
            if (Manager == null || Manager.Navigator == null)
            {
                return;
            }

            Manager.ResetSuspicion();

            Transform patrolPoint = Manager.GetCurrentPatrolPoint();
            if (patrolPoint != null)
            {
                Manager.Navigator.SetDestination(patrolPoint.position, true);
            }
        }

        public override void TickState()
        {
            if (Manager == null)
            {
                return;
            }

            if (Manager.IsPlayerDetected())
            {
                Manager.UpdateLastKnownPlayerPosition();
                Manager.ResetSuspicion();
                Manager.UpdateState(Manager.SuspiciousState);
                return;
            }

            GuardNavigator navigator = Manager.Navigator;
            if (navigator == null)
            {
                return;
            }

            navigator.TickAdvance();
            if (navigator.ReachedDestination)
            {
                Transform nextPoint = Manager.AdvancePatrolPoint();
                if (nextPoint != null)
                {
                    navigator.SetDestination(nextPoint.position, true);
                }
            }
        }

        public override void ExitState()
        {
        }
    }
}
