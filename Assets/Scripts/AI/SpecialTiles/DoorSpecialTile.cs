using HeistGame.Door;
using UnityEngine;

[RequireComponent(typeof(DoorController))]
public class DoorSpecialTile : MonoBehaviour, ISpecialTile
{
    [SerializeField] private bool allowUnlock = true;

    private DoorController doorController;
    private IDoorOpenBehavior openBehavior;
    private IDoorLockBehavior lockBehavior;
    private IDoorDestroyBehavior destroyBehavior;

    private void Awake()
    {
        doorController = GetComponent<DoorController>();
        openBehavior = GetComponent<IDoorOpenBehavior>();
        lockBehavior = GetComponent<IDoorLockBehavior>();
        destroyBehavior = GetComponent<IDoorDestroyBehavior>();
    }

    public bool CanPass()
    {
        if (destroyBehavior != null && destroyBehavior.IsDestroyed)
        {
            return true;
        }

        if (openBehavior != null && openBehavior.IsOpen)
        {
            return true;
        }

        if (lockBehavior != null && lockBehavior.IsLocked && !allowUnlock)
        {
            return false;
        }

        return true;
    }

    public void OnPass()
    {
        doorController?.TryOpenDoor();
    }
}
