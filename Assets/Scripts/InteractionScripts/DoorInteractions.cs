using System.Collections.Generic;
using UnityEngine;
using HeistGame.Door;

namespace HeistGame.Interactions {
    public class DoorInteractions : InteractArea {
        
        private void Start() {
            buttons.Clear(); 

            InteractBtnTemplate enterDoor = new InteractBtnTemplate();
            enterDoor.text = "Open Door";
            enterDoor.onClick.AddListener(EnterDoor);
            buttons.Add(enterDoor);
        }

        private void EnterDoor() {
            bool success = TryOpenDoor();
            if (!success) {
                Debug.LogWarning("Failed to open door. Check if it's locked or destroyed.");
                return;
            }
        }
    }
}