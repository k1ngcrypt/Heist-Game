using UnityEngine;
using System.Collections.Generic;

namespace HeistGame.Objectives {
    public class LevelObjectiveLoader : MonoBehaviour{
        // Drag and drop ONLY the objectives for this specific level here in the Inspector
        [SerializeField] private List<ObjectiveData> levelObjectives = new();

        private void Start() {
            // Hand the list over to the manager
            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.InitializeLevelObjectives(levelObjectives); 
            else Debug.LogError("ObjectiveManager missing from the scene! Cannot load objectives.");
        }
    }
}