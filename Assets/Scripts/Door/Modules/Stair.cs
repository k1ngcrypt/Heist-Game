using UnityEngine;

namespace HeistGame.Door {
    public class Stair : MonoBehaviour, IDoorOpenBehavior, IHaSpecialLink {
        [SerializeField] private Transform otherStair;
        [SerializeField] private int traversalCost = 9;
        private GameObject player;
        public bool IsOpen => true;
        public Transform LinkTransform => transform;
        public Transform OtherLinkTransform => otherStair;
        public bool IsBidirectional => true;
        public int TraversalCost => traversalCost;
         
        private void OnEnable() {
            player = GameObject.FindWithTag("Player");
            Pathfinder.RegisterSpecialLink(this);
        }

        private void OnDisable() {
            Pathfinder.UnregisterSpecialLink(this);
        }
        public void OpenDoor() {
            player.transform.position = Map.AlignToObjectPos(otherStair.position);
        }

        public void CloseDoor() {
            // Stairs don't close, so this can be left empty or used for any cleanup if necessary.
        }
    }
}
