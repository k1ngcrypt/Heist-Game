using UnityEngine;

namespace HeistGame.Door
{
    public class Openable : MonoBehaviour, IDoorOpenBehavior
    {
        [SerializeField] private bool isOpen;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite closeSprite;

        public bool IsOpen => isOpen;

        public void OpenDoor()
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;
        }

        public void CloseDoor()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
        }
    }
}
