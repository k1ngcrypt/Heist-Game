using UnityEngine;

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