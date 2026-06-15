using UnityEngine;

namespace HeistGame.Objectives{
    public class ObjectiveTrigger : MonoBehaviour {
        [Header("Base Objective Settings")]
        [Tooltip("Matches the objectiveID in your ObjectiveData ScriptableObject.")]
        [SerializeField] protected int targetObjectiveID;
        [SerializeField] protected int progressAmount = 1;
        [SerializeField] protected int prerequisiteID = -1;

        public virtual void TriggerProgress() {
            if (targetObjectiveID == 0) {
                Debug.LogWarning($"ObjectiveTrigger on {gameObject.name} is missing an ID!");
                return;
            }

            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.UpdateObjectiveProgress(targetObjectiveID, progressAmount);
        }
    }
}