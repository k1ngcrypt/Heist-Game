namespace Guards
{
    public class SearchingState : BaseState
    {
        public override void EnterState()
        {
            if (Manager == null || Manager.Navigator == null)
            {
                return;
            }
            _icon.TriggerSuspicious();
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
                Manager.UpdateState(Manager.PatrollingState);
            }
        }

        public override void ExitState()
        {
            _icon.HideIcon();
        }
    }
}
