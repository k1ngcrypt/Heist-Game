using UnityEngine;

namespace HeistGame.Objectives {
    public class CollectionObjective : ObjectiveTrigger {
        public override void TriggerProgress() {
            base.TriggerProgress();

            Debug.Log($"{gameObject.name} added to inventory.");
            Destroy(gameObject);
        }
    }
}