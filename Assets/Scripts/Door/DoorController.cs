using UnityEngine;

namespace HeistGame.Door
{
    [RequireComponent(typeof(IDoorOpenBehavior))]
    [RequireComponent(typeof(IDoorLockBehavior))]
    [RequireComponent(typeof(IDoorDestroyBehavior))]
    public class DoorController : MonoBehaviour, ISpecialTile, IPathfindingPassthrough
    {
        private IDoorOpenBehavior openBehavior;
        private IDoorLockBehavior lockBehavior;
        private IDoorDestroyBehavior destroyBehavior;
        private AutoCloser autoCloser;
        [SerializeField] private bool isDoor = false;
        public bool CanPass() => true;
        public bool IsDoor() => isDoor;
        public void OnApproach()
        {
            if (openBehavior.IsOpen) return;
            TryOpenDoor();
        }

        public void OnClear()
        {
            if (!openBehavior.IsOpen) return;
            TryCloseDoor();
        }

        private void Awake()
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
            Debug.Log($"Attempting to open door {gameObject.name}");
            if (destroyBehavior != null && destroyBehavior.IsDestroyed)
            {
                Debug.Log("Door blocked: already destroyed");
                return false;
            }

            if (lockBehavior != null && lockBehavior.IsLocked && !lockBehavior.TryUnlock())
            {
                Debug.Log("Door blocked: locked");
                return false;
            }

            if (openBehavior == null)
            {
                Debug.Log($"Door blocked: openBehavior is null on {gameObject.name}");
                return false;
            }

            openBehavior.OpenDoor();
            autoCloser?.NotifyDoorOpened();
            return true;
        }

        public bool TryCloseDoor()
        {
            Debug.Log($"Attempting to close door {gameObject.name}");
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

            bool destroyed = destroyBehavior.TryDestroy();
            if (destroyed)
            {
                Pathfinder.NotifyObstacleChanged(transform.position);
            }

            return destroyed;
        }
    }
}
