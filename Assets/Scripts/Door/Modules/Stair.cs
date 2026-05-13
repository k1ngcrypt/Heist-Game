using UnityEngine;

namespace HeistGame.Door {
    public class Stair : MonoBehaviour, IDoorOpenBehavior {
        [SerializeField] private Transform otherStair;
        private GameObject player;
        public bool IsOpen => true;
         
        private void OnEnable() {
            player = GameObject.FindWithTag("Player");
        }
        public void OpenDoor() {
            player.transform.position = otherStair.position;
        }

        public void CloseDoor() {
            // Stairs don't close, so this can be left empty or used for any cleanup if necessary.
        }
    }
}
