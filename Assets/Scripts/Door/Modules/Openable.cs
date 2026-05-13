using UnityEngine;

namespace HeistGame.Door
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Openable : MonoBehaviour, IDoorOpenBehavior
    {
        [SerializeField] private bool isOpen;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite closeSprite;

        private SpriteRenderer spriteRenderer;

        public bool IsOpen => isOpen;

        private void OnValidate()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (closeSprite != null)
            {
                spriteRenderer.sprite = closeSprite;
            }else if (openSprite != null)
            {
                spriteRenderer.sprite = openSprite;
            }
        }

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
         

        public void OpenDoor()
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;
            spriteRenderer.sprite = openSprite;
            gameObject.layer = LayerMask.NameToLayer("Default");
        }

        public void CloseDoor()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            spriteRenderer.sprite = closeSprite;
            gameObject.layer = LayerMask.NameToLayer("Obstacle");
        }
    }
}
