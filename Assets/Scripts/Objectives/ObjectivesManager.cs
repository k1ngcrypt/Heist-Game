using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeistGame.Objectives {
    public class ObjectiveManager : MonoBehaviour {
        public static ObjectiveManager Instance { get; private set; }

        // Eddie looks at This
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
            foreach (var data in levelData) activeObjectives.Add(new ActiveObjective(data, data.startVisible));
            OnObjectivesChanged?.Invoke();
        }

        // For Eddie: Call this to get the list of objectives that should be shown on the UI for the specific level.
        public List<ActiveObjective> GetVisibleObjectives() {
            List<ActiveObjective> visibleList = new();
            foreach (var obj in activeObjectives) if (obj.IsVisible) visibleList.Add(obj);
            return visibleList;
        }

        // Called when the player completes an action
        public void UpdateObjectiveProgress(string objectiveID, int progressAmount = 1) {
            ActiveObjective target = activeObjectives.Find(o => o.Data.objectiveID == objectiveID);

            if (target != null) {
                bool newlyCompleted = target.AdvanceProgress(progressAmount);
                OnObjectivesChanged?.Invoke();

                if (newlyCompleted) Debug.Log($"Logic: {target.Data.title} is complete.");
            }
        }

        // Call this to reveal "additional/hidden" objectives (Like if the player is an idiot and attacks the guards, the objectives now change)
        public void RevealObjective(string objectiveID) {
            ActiveObjective target = activeObjectives.Find(o => o.Data.objectiveID == objectiveID);
            if (target != null && !target.IsVisible) {
                target.Reveal();
                OnObjectivesChanged?.Invoke(); // Tell the UI to redraw because a new objective appeared
            }
        }
    }
}