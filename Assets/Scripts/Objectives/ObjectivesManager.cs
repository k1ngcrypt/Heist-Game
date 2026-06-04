using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeistGame.Objectives {
    public class ObjectiveManager : MonoBehaviour {
        public static ObjectiveManager Instance { get; private set; }

        // Eddie looks at this for the UI
        public static event Action OnObjectivesChanged; 

        // The master list of runtime objectives for the current level
        private readonly List<ActiveObjective> activeObjectives = new();

        private void Awake() {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // Called by LevelObjectiveLoader when the scene boots up
        public void InitializeLevelObjectives(List<ObjectiveData> levelData) {
            activeObjectives.Clear();
            foreach (var data in levelData) activeObjectives.Add(new ActiveObjective(data));
            OnObjectivesChanged?.Invoke();
        }

        // For Eddie: Call this to get the list of objectives that should be shown on the UI for the specific level.
        public List<ActiveObjective> GetVisibleObjectives() {
            List<ActiveObjective> visibleList = new();
            foreach (var obj in activeObjectives) if (!obj.IsHidden) visibleList.Add(obj);
            return visibleList;
        }

        // Called when the player completes an action
        public void UpdateObjectiveProgress(int objectiveID, int progressAmount = 1) {
            
            ActiveObjective target = activeObjectives.Find(o => o.Data.objectiveID == objectiveID);
            if (target != null) {
                if (target.IsFailed) {
                    Debug.LogWarning($"Objective {target.Data.title} has already failed. Progress cannot be updated.");
                    return;
                }
                
                bool newlyCompleted = target.AdvanceProgress(progressAmount);
                OnObjectivesChanged?.Invoke();

                if (newlyCompleted) Debug.Log($"Logic: {target.Data.title} is complete.");
            }
        }

        // Call this when the player fails an objective (e.g., gets caught by a guard)
        public void FailObjective(int objectiveID) {
            ActiveObjective target = activeObjectives.Find(o => o.Data.objectiveID == objectiveID);

            if (target != null) {
                if (!target.Data.failable && !target.Data.isOptional) {
                    Debug.LogWarning($"Objective {target.Data.title} cannot be failed!");
                    return;
                }
                
                bool newlyFailed = target.Fail();
                OnObjectivesChanged?.Invoke();

                if (newlyFailed) {
                    if (!target.Data.isOptional) Debug.Log($"Logic: {target.Data.title} has failed. Player has failed the level."); //Edit code to make it fail level when this happens
                    else Debug.Log($"Logic: {target.Data.title} has failed. However Player can still pass the level");
                }
            }
        }

        // Call this to reveal "additional/hidden" objectives
        public void RevealObjective(int objectiveID) {
            ActiveObjective target = activeObjectives.Find(o => o.Data.objectiveID == objectiveID);
            if (target != null && target.IsHidden) {
                target.Reveal();
                OnObjectivesChanged?.Invoke(); // Tell the UI to redraw because a new objective appeared
            }
        }
    }
}
