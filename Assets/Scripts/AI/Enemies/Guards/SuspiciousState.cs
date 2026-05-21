namespace Guards
{
    public class SuspiciousState : BaseState
    {
        public override void EnterState()
        {
            if (Manager == null || Manager.Navigator == null)
            {
                return;
            }

            if (Manager.IsPlayerDetected())
            {
                Manager.UpdateLastKnownPlayerPosition();
                Manager.Navigator.SetDestination(Manager.PlayerTarget.position, true);
                return;
            }

            Manager.Navigator.SetDestination(Manager.LastKnownPlayerPosition, true);
        }

        public override void TickState()
        {
            if (Manager == null || Manager.Navigator == null)
            {
                return;
            }

            if (Manager.IsPlayerDetected())
            {
                Manager.UpdateLastKnownPlayerPosition();
                
                Manager.Navigator.SetDestination(Manager.PlayerTarget.position, true);

                if (Manager.IncreaseSuspicion())
                {
                    Manager.UpdateState(Manager.ChasingState);
                    return;
                }

                Manager.Navigator.TickAdvance();
                return;
            }

            Manager.Navigator.TickAdvance();
            if (Manager.Navigator.ReachedDestination)
            {
                Manager.UpdateState(Manager.SearchingState);
            }
        }

        public override void ExitState()
        {
        }
    }
}
