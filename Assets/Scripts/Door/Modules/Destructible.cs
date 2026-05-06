using UnityEngine;

namespace HeistGame.Door
{
    public class Destructible : MonoBehaviour, IDoorDestroyBehavior
    {
        [SerializeField] private bool isDestroyed;

        public bool IsDestroyed => isDestroyed;

        public bool TryDestroy()
        {
            if (isDestroyed)
            {
                return false;
            }

            isDestroyed = true;
            return true;
        }
    }
}
