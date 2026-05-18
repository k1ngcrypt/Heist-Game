using System;

namespace HeistGame.Objectives {
    [Serializable]
    public class ActiveObjective {
        public ObjectiveData Data { get; private set; }
        public int CurrentAmount { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsVisible { get; private set; }

        public ActiveObjective(ObjectiveData data, bool startVisible) {
            Data = data;
            CurrentAmount = 0;
            IsCompleted = false;
            IsVisible = startVisible;
        }

        public bool AdvanceProgress(int amount) {
            if (IsCompleted) return false;

            CurrentAmount = Math.Min(CurrentAmount + amount, Data.requiredAmount);
            
            if (CurrentAmount >= Data.requiredAmount) {
                IsCompleted = true;
                return true;
            }
            return false;
        }

        public void Reveal() { IsVisible = true; }
    }
}