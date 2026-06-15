using UnityEngine;

namespace HeistGame.Door
{
    public class Destructible : MonoBehaviour, IDoorDestroyBehavior
    {
        [SerializeField] private int[] requiredKeyIds = {-1};
        [SerializeField] private bool isDestroyed;
        [SerializeField] private Sprite destroyedSprite;

        public bool IsDestroyed => isDestroyed;

        public bool TryDestroy(int itemID)
        {
            if (isDestroyed) return false;

            foreach (int validID in requiredKeyIds) {
                if (validID == itemID) {
                    isDestroyed = true;
                    GetComponent<SpriteRenderer>().sprite = destroyedSprite;
                    InteractArea interactArea;
                    foreach (Transform child in transform) {
                        interactArea = child.GetComponent<InteractArea>();
                        if (interactArea != null) {
                            interactArea.transform.SetParent(null); 
                            Destroy(interactArea.gameObject);
                        }
                    }
                    gameObject.layer = LayerMask.NameToLayer("Pain");
                    return true;
                }
            }
            
            return false;
        }
    }
}
