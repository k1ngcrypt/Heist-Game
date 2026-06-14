using UnityEngine;
//UNIMPLEMENTED!! Player inventory is not yet handled
namespace HeistGame.Door
{
    public class Lockable : MonoBehaviour, IDoorLockBehavior
    {
        [SerializeField] private int[] requiredKeyIds = {-1};
        [SerializeField] private bool isLocked = true;
        public bool IsLocked => isLocked;

        public bool TryLock()
        {
            if (isLocked) return false;

            isLocked = true;
            return true;
        }

        public bool TryUnlock(int itemID) {
            if (!isLocked) return true;
            
            foreach (int validID in requiredKeyIds) {
                if (validID == itemID) {
                    isLocked = false;
                    return true;
                }
            }
            return false;
        }
    }
}
