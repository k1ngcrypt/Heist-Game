using Guards;
using UnityEngine;

namespace Guards
{
    public class ChasingState : BaseState
    {
        const float CHASING_INCREMENT = 10f;
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
                var dist = Vector2.Distance(transform.position, Manager.PlayerTarget.position);
                if (dist <= 1.9f) //Large epsilon
                {
                    SceneUIManager.Instance.MissionFailed("You were caught by a guard!");
                    return;
                } else if (dist <= Manager.attacker.attackRange)
                {
                    Manager.attacker.Attack();
                }

                if (Manager.PlayerTarget != null)
                {
                    AwarenessManager.Instance.ReportGuardSuspicion(CHASING_INCREMENT);
                    navigator.SetDestination(Manager.PlayerTarget.position, true);
                }

            }
            else if (navigator.ReachedDestination)
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
