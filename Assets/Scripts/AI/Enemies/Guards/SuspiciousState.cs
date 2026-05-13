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

            Manager.Navigator.SetDestination(Manager.LastKnownPlayerPosition, true);
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
                Manager.UpdateState(Manager.ChasingState);
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
                Manager.UpdateState(Manager.SearchingState);
            }
        }

        public override void ExitState()
        {
        }
    }
}
