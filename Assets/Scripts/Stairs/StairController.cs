using UnityEngine;

namespace HeistGame.Stair {
    public class StairController : MonoBehaviour {
        private IStairTravelBehavior travelBehavior;
        private IStairLockBehavior lockBehavior;
        private void OnEnable () {
            travelBehavior = GetComponent<IStairTravelBehavior>();
            lockBehavior = GetComponent<IStairLockBehavior>();
        }

        public bool TryUseStairs(GameObject user) {
            if (lockBehavior != null && lockBehavior.IsLocked) {
                if (!lockBehavior.TryUnlock()) {
                    Debug.Log("Failed to unlock stairs!");
                    return false;
                }
            }
            if (travelBehavior != null) {
                travelBehavior.Transport(user);
                return true;
            }
            Debug.LogWarning("No travel behavior found on stairs!");
            return false;
        }
    }
}


