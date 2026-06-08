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
            if (Manager == null || Manager.Navigator == null)
            {
                return;
            }

            if (Manager.IsPlayerDetected())
            {
                Manager.Navigator.SetDestination(Manager.LastKnownPlayerPosition, true);
                Manager.UpdateState(Manager.SuspiciousState);
                return;
            }

            if (Manager.CheckForAnomalies() != null || Manager.CheckForCorpses() != null)
            {
                Manager.Navigator.SetDestination(Manager.LastKnownAnomalyPosition, true);
                Manager.UpdateState(Manager.SuspiciousState);
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
