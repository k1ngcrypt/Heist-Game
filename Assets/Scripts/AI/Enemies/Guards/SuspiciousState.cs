using UnityEngine;

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

            _icon.TriggerSuspicious();
            Manager.SetBaseSuspicion();

            if (Manager.IsPlayerDetected())
            {
                Manager.Navigator.SetDestination(Manager.PlayerTarget.position, true);
                return;
            }

            Manager.Navigator.TickAdvance();
        }

        public override void TickState()
        {
            if (Manager == null || Manager.Navigator == null)
            {
                return;
            }

            if (Manager.IsPlayerDetected())
            {
                Manager.Navigator.SetDestination(Manager.PlayerTarget.position, true);

                Manager.IncreaseSuspicion();
            }

            Manager.Navigator.TickAdvance();
            if (Manager.Navigator.ReachedDestination)
            {
                if (Vector2.Distance(Manager.Navigator.transform.position, Manager.LastKnownAnomalyPosition) < 0.4f)
                { //epsilon
                    var bag = Manager.CurrentAnomaly.GetComponent<BagUI>();
                    AwarenessManager.Instance.AnomalyIncrement(bag.bagItems);
                    bag.DestroyBag();
                }
                if (Manager.AwarenessLevel() >= AwarenessLevel.Alert) Manager.UpdateState(Manager.SearchingState);
                Manager.ResetSuspicion();
                Manager.UpdateState(Manager.PatrollingState);
            }
        }

        public override void ExitState()
        {
            _icon.HideIcon();
        }
    }
}
