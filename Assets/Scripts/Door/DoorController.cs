using UnityEngine;
public class DoorController : MonoBehaviour
{
    private IDoorOpenBehavior openBehavior;
    private IDoorLockBehavior lockBehavior;
    private IDoorDestroyBehavior destroyBehavior;
    private AutoCloser autoCloser;

    private void OnEnable()
    {
        CacheBehaviors();
    }

    private void CacheBehaviors()
    {
        openBehavior = GetComponent<IDoorOpenBehavior>();
        lockBehavior = GetComponent<IDoorLockBehavior>();
        destroyBehavior = GetComponent<IDoorDestroyBehavior>();
        autoCloser = GetComponent<AutoCloser>();
    }

    public bool TryOpenDoor()
    {
        if (destroyBehavior != null && destroyBehavior.IsDestroyed)
        {
            return false;
        }

        if (lockBehavior != null && lockBehavior.IsLocked && !lockBehavior.TryUnlock())
        {
            return false;
        }

        if (openBehavior == null)
        {
            return false;
        }

        openBehavior.OpenDoor();
        autoCloser?.NotifyDoorOpened();
        return true;
    }

    public bool TryCloseDoor()
    {
        if (openBehavior == null)
        {
            return false;
        }

        openBehavior.CloseDoor();
        autoCloser?.NotifyDoorClosed();
        return true;
    }

    public bool TryDestroyDoor()
    {
        if (destroyBehavior == null)
        {
            return false;
        }

        return destroyBehavior.TryDestroy();
    }
}
