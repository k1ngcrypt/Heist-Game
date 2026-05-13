namespace Guards
{
    public class IdleState : BaseState
    {
        public override void EnterState()
        {
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
                Manager.UpdateState(Manager.ChasingState);
            }
        }

        public override void ExitState()
        {
        }
    }
}
