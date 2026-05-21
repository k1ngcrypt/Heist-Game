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

            if (Manager.Navigator == null)
            {
                return;
            }

            Manager.Navigator.TickAdvance();
            if (Manager.Navigator.ReachedDestination)
            {
                Transform nextPoint = Manager.AdvancePatrolPoint();
                if (nextPoint != null)
                {
                    Manager.Navigator.SetDestination(nextPoint.position, true);
                }
            }
        }

        public override void ExitState()
        {
        }
    }
}
