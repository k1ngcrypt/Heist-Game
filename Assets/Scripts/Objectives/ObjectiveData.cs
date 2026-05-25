using UnityEngine;

namespace HeistGame.Objectives {
    [CreateAssetMenu(fileName = "NewObjective", menuName = "Heist Game/Objective")]
    public class ObjectiveData : ScriptableObject {
        [Header("Display Information")]
        public string objectiveID;
        public string title;
        [TextArea] public string description;

        [Header("Progress Settings")]
        public bool manyRequiredTasks; // True if it needs multiple things done (e.g., steal 3 paintings)
        public int requiredAmount = 1; // Amount needed to complete (default 1 for single tasks)
        public bool isHidden = false; // If true, this objective starts hidden and must be revealed through gameplay (e.g., "Find the hidden safe")
        public bool isOptional = false; // Optional objectives can be ignored without failing the level
        public bool failable = false; // If true, failing this objective causes the player to fail the level (e.g., get caught by guards)
    }
}