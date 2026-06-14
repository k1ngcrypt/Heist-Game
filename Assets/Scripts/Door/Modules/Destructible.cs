using UnityEngine;

namespace HeistGame.Door
{
    public class Destructible : MonoBehaviour, IDoorDestroyBehavior
    {
        [SerializeField] private int[] requiredKeyIds = {-1};
        [SerializeField] private bool isDestroyed;

        public bool IsDestroyed => isDestroyed;

        public bool TryDestroy(int itemID)
        {
            if (isDestroyed) return false;

            foreach (int validID in requiredKeyIds) {
                if (validID == itemID) {
                    isDestroyed = true;
                    return true;
                }
            }
            
            return false;
        }
    }
}
