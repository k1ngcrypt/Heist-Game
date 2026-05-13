using UnityEngine;

namespace HeistGame.Door {
    [RequireComponent(typeof(SpriteRenderer))]
    public class Vent : MonoBehaviour, IDoorOpenBehavior {
        [SerializeField] private bool isOpen;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite closeSprite;
        [SerializeField] private Transform otherVent;
        private GameObject player;

        private SpriteRenderer spriteRenderer;

        public bool IsOpen => isOpen;

        private void OnValidate() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (closeSprite != null) spriteRenderer.sprite = closeSprite;
            else if (openSprite != null) spriteRenderer.sprite = openSprite;
        }

        private void OnEnable() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            player = GameObject.FindWithTag("Player");
        }
         
        public void OpenDoor() {
            isOpen = true;
            spriteRenderer.sprite = openSprite;
            player.transform.position = otherVent.position;
        }

        public void CloseDoor() {
            if (!isOpen) return; // Prevent closing if already closed
            isOpen = false;
            spriteRenderer.sprite = closeSprite;
        }
    }
}
