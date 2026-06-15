using UnityEngine;

namespace HeistGame.Door
{
    public class Indestructible : MonoBehaviour, IDoorDestroyBehavior
    {
        private readonly bool isDestroyed = false;

        public bool IsDestroyed => isDestroyed;

        public bool TryDestroy(int itemID)
        {
            return false;
        }
    }
}
