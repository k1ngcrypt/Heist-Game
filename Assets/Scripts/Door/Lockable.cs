using UnityEngine;

public class Lockable : MonoBehaviour, IDoorLockBehavior
{
    [SerializeField] private int requiredKeyId;
    [SerializeField] private bool isLocked = true;

    public bool IsLocked => isLocked;

    public bool TryLock()
    {
        if (isLocked)
        {
            return false;
        }

        isLocked = true;
        return true;
    }

    public bool TryUnlock()
    {
        if (!isLocked)
        {
            return true;
        }

        isLocked = false;
        return true;
    }
}
