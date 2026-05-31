using UnityEngine;
using HeistGame.Objectives;
public class ObjectiveList : MonoBehaviour {
    [SerializeField] private Transform containerParent;
    [SerializeField] private ObjectiveItem objectiveItemPrefab;
    [SerializeField] private ObjectiveManager objectiveManager;

    private void OnEnable() { ObjectiveManager.OnObjectivesChanged += RefreshObjectiveList; }
    private void OnDisable(){ ObjectiveManager.OnObjectivesChanged -= RefreshObjectiveList; }
    private void Start() { RefreshObjectiveList(); }

    private void RefreshObjectiveList() {
        if (objectiveManager == null || containerParent == null || objectiveItemPrefab == null) return;

        // 1. Wipe old text UI elements cleanly
        foreach (Transform child in containerParent) Destroy(child.gameObject);

        // 2. Ask the manager for ONLY the currently visible objectives
        var visibleObjectives = objectiveManager.GetVisibleObjectives();

        // 3. Populate the UI layout with updated rows
        foreach (var objective in visibleObjectives) {
            ObjectiveItem newItem = Instantiate(objectiveItemPrefab, containerParent);
            
            newItem.Setup(objective.Data.title, objective.CurrentAmount,
            objective.Data.requiredAmount, objective.IsCompleted, objective.IsFailed);
        }
    }
}