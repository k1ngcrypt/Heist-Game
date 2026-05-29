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

            Vector2? patrolPoint = Manager.GetCurrentPatrolPoint();
            if (patrolPoint.HasValue)
            {
                Manager.Navigator.SetDestination(patrolPoint.Value, true);
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
                Vector2? nextPoint = Manager.AdvancePatrolPoint();
                if (nextPoint.HasValue)
                {
                    Manager.Navigator.SetDestination(nextPoint.Value, true);
                }
            }
        }

        public override void ExitState()
        {
        }
    }
}
