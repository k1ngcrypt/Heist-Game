using UnityEngine;

namespace HeistGame.Objectives {
    public class CollectionObjective : ObjectiveTrigger {
        public override bool TriggerProgress() {
            if (!base.TriggerProgress()) return false;

            Debug.Log($"{gameObject.name} added to inventory.");
            gameObject.transform.SetParent(null); 
            Destroy(gameObject);
            return true;
        }
    }
}