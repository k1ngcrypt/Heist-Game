using System.Collections.Generic;
using UnityEngine;

namespace HeistGame.Door
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Openable : MonoBehaviour, IDoorOpenBehavior
    {
        [SerializeField] private bool isOpen;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite closeSprite;
        [SerializeField] private BoxCollider2D doorShadowCollider;

        private SpriteRenderer spriteRenderer;

        public bool IsOpen => isOpen;

        private void OnValidate()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (closeSprite != null)
            {
                spriteRenderer.sprite = closeSprite;
                List<Vector2> closeSpriteVertices = new(); 
                closeSprite.GetPhysicsShape(0,closeSpriteVertices);
                if (closeSpriteVertices == null || closeSpriteVertices.Count == 0) closeSpriteVertices = new List<Vector2>(closeSprite.vertices);
                if (doorShadowCollider !=null && closeSpriteVertices != null && closeSpriteVertices.Count != 0) {
                    Vector2 AverageVertex = Vector2.zero, MinVertex = closeSpriteVertices[0], MaxVertex = closeSpriteVertices[0];
                    foreach (Vector2 v in closeSpriteVertices) {
                        AverageVertex+=v;
                        MinVertex = new Vector2(Mathf.Min(MinVertex.x,v.x),Mathf.Min(MinVertex.y,v.y));
                        MaxVertex = new Vector2(Mathf.Max(MaxVertex.x,v.x),Mathf.Max(MaxVertex.y,v.y));
                    }
                    doorShadowCollider.offset = AverageVertex/closeSpriteVertices.Count;
                    doorShadowCollider.size = MaxVertex-MinVertex;
                }
            }else if (openSprite != null)
            {
                spriteRenderer.sprite = openSprite;
            }
        }

        private void Awake()
        {
            if (doorShadowCollider!=null) doorShadowCollider.enabled = !isOpen;
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
            if (doorShadowCollider != null) doorShadowCollider.enabled = false;
            gameObject.layer = LayerMask.NameToLayer("Pain");
        }

        public void CloseDoor()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            spriteRenderer.sprite = closeSprite;
            if (doorShadowCollider != null) doorShadowCollider.enabled = true;
            gameObject.layer = LayerMask.NameToLayer("Obstacle");
        }
    }
}
