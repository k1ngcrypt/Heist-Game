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
        public bool startVisible = true;
    }
}