using UnityEngine;

namespace HeistGame.Door
{
    public interface IDoorOpenBehavior
    {
        bool IsOpen { get; }
        void OpenDoor();
        void CloseDoor();
    }

    public interface IDoorLockBehavior
    {
        bool IsLocked { get; }
        bool TryUnlock(); // Returns true if the player has the right key/beats the minigame
        bool TryLock();
    }

    public interface IDoorDestroyBehavior
    {
        bool IsDestroyed { get; }
        bool TryDestroy(); // Returns true if the player has the right tool/beats the minigame
    }

    public interface IHaSpecialLink
    {
        Transform LinkTransform { get; }
        Transform OtherLinkTransform { get; }
        bool IsBidirectional { get; }
        int TraversalCost { get; }
    }
}
