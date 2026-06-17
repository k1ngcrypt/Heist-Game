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
                if (Vector2.Distance(Manager.Navigator.transform.position, Manager.PlayerTarget.position) < 1.2f) Manager.UpdateState(Manager.SearchingState);
                Manager.Navigator.TickAdvance();
                return;
            }
            
            if (Vector2.Distance(Manager.Navigator.transform.position, Manager.PlayerTarget.position) < 0.4f) Manager.UpdateState(Manager.SearchingState);
            Manager.Navigator.TickAdvance();
            if (Manager.Navigator.ReachedDestination)
            {
                if (Vector2.Distance(Manager.Navigator.transform.position, Manager.LastKnownAnomalyPosition) < 0.4f)
                { //epsilon
                    if (Manager.CurrentAnomaly.TryGetComponent(out BloodPuddle bloodPuddle))
                    {
                        AwarenessManager.Instance.CorpseFound();
                        Destroy(bloodPuddle.gameObject);
                    } else
                    {
                        var bag = Manager.CurrentAnomaly.GetComponent<BagUI>();
                        AwarenessManager.Instance.AnomalyIncrement(bag.bagItems);
                        bag.DestroyBag();
                    }
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
