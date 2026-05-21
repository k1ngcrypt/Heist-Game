using UnityEngine;

namespace HeistGame.Door
{
    [RequireComponent(typeof(IDoorOpenBehavior))]
    [RequireComponent(typeof(IDoorLockBehavior))]
    [RequireComponent(typeof(IDoorDestroyBehavior))]
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
            Pathfinder.NotifyObstacleChanged(transform.position);
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
            Pathfinder.NotifyObstacleChanged(transform.position);
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
