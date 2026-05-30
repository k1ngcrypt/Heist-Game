using System;

namespace HeistGame.Objectives {
    [Serializable]
    public class ActiveObjective {
        public ObjectiveData Data { get; private set; }
        public int CurrentAmount { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsHidden { get; private set; }
        public bool IsFailed { get; private set; }

        public ActiveObjective(ObjectiveData data) {
            Data = data;
            CurrentAmount = 0;
            IsCompleted = false;
            IsHidden = data.isHidden;
            if (data.failable || data.isOptional) IsFailed = false;
        }

        public bool AdvanceProgress(int amount) {
            if (IsCompleted || IsFailed) return false;

            CurrentAmount += amount; 
            if (CurrentAmount >= Data.requiredAmount) {
                IsCompleted = true;
                return true;
            }
            return false;
        }

        public bool Fail() {
            if (!Data.failable || IsFailed) return false; // Can't fail if it's not failable or already failed
            IsFailed = true;
            return true;
        }

        public void Reveal() { IsHidden = false; }
    }
}