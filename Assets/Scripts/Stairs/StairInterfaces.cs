using UnityEngine;
namespace HeistGame.Stair {
    public interface IStairTravelBehavior{
        void Transport(GameObject user);
    }

    public interface IStairLockBehavior {
        bool IsLocked { get; }
        bool TryUnlock();
        bool TryLock();
    }
}
