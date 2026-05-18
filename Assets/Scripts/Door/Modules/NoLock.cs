using UnityEngine;

namespace HeistGame.Door
{
    public class NoLock : MonoBehaviour, IDoorLockBehavior
    {
        private readonly bool isLocked = false;

        public bool IsLocked => isLocked;

        public bool TryLock()
        {
            return false;
        }

        public bool TryUnlock()
        {
            return true;
        }
    }
}
