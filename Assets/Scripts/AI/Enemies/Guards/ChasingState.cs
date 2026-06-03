using UnityEngine;

namespace Guards
{
    public class ChasingState : BaseState
    {
        public override void EnterState()
        {
            if (Manager == null || Manager.Navigator == null)
            {
                return;
            }

            if (Manager.PlayerTarget != null)
            {
                Manager.Navigator.SetDestination(Manager.PlayerTarget.position, true);
            }
            _icon.TriggerAlerted();
        }

        public override void TickState()
        {
            if (Manager == null)
            {
                return;
            }

            GuardNavigator navigator = Manager.Navigator;
            if (navigator == null)
            {
                return;
            }

            if (Manager.IsPlayerDetected())
            {
                if (Manager.PlayerTarget != null)
                {
                    navigator.SetDestination(Manager.PlayerTarget.position, true);
                }

            } else if (navigator.ReachedDestination)
            {
                Manager.UpdateState(Manager.SearchingState);
                return;
            }
            navigator.TickAdvance();

        }

        public override void ExitState()
        {
            _icon.HideIcon();
        }
    }
}
