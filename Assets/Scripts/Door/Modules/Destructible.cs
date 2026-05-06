using UnityEngine;

namespace HeistGame.Door
{
    public class Destructible : MonoBehaviour, IDoorDestroyBehavior
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private bool isDestroyed;

        public bool IsDestroyed => isDestroyed;

        private void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            if (currentHealth == 0)
            {
                isDestroyed = true;
            }
        }

        public bool TryDestroy()
        {
            if (isDestroyed)
            {
                return false;
            }

            currentHealth = 0;
            isDestroyed = true;
            return true;
        }
    }
}
