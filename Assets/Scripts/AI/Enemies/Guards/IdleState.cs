namespace Guards
{
    public class IdleState : BaseState
    {
        public override void EnterState()
        {
            Manager?.ResetSuspicion();
            Manager?.Navigator?.ClearDestination();
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
            }
        }

        public override void ExitState()
        {
        }
    }
}
