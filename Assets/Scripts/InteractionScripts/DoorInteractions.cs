using System.Collections.Generic;
using UnityEngine;
using HeistGame.Door;

namespace HeistGame.Interactions {
    public class DoorInteractions : MonoBehaviour, IInteractionContributor {
        private DoorController door;
        private IDoorOpenBehavior openBehavior;
        private IDoorLockBehavior lockBehavior;
        private IDoorDestroyBehavior destroyBehavior;
        private InteractionStateManager stateManager;
 
        [SerializeField] private bool openable;
        [SerializeField] private bool lockable;
        [SerializeField] private bool destroyable;

        private void Awake() {
            FetchDependencies();
        }

        private void FetchDependencies() {
            door = GetComponentInParent<DoorController>();
            openBehavior = door.GetComponent<IDoorOpenBehavior>();
            lockBehavior = door.GetComponent<IDoorLockBehavior>();
            destroyBehavior = door.GetComponent<IDoorDestroyBehavior>();
            stateManager = GetComponent<InteractionStateManager>();
        }

        public List<InteractBtnTemplate> GetContextButtons() {
            List<InteractBtnTemplate> myButtons = new List<InteractBtnTemplate>();

            if (openable) {
                if (openBehavior != null) {
                    string openText = openBehavior.IsOpen? "Close Door" : "Open Door";
                    
                    InteractBtnTemplate openBtn = new InteractBtnTemplate();
                    openBtn.text = openText;
                    openBtn.onClick.AddListener(InteractWithDoor);
                    myButtons.Add(openBtn);
                }
            }

            if (lockable) {
                if (lockBehavior != null) {
                string lockText = lockBehavior.IsLocked? "Unlock Door" : "Lock Door";

                    InteractBtnTemplate lockBtn = new InteractBtnTemplate();
                    lockBtn.text = lockText;
                    lockBtn.onClick.AddListener(UnlockDoor);
                    myButtons.Add(lockBtn);
                }
            }

            if (destroyable) {
                InteractBtnTemplate destroyBtn = new InteractBtnTemplate();
                destroyBtn.text = "Break Door";
                destroyBtn.onClick.AddListener(DestroyDoor);
                myButtons.Add(destroyBtn);
            }
            return myButtons;
        }

        private void InteractWithDoor() {
            bool success;
            if (openBehavior.IsOpen) {
                success = door.TryCloseDoor();
                if (success) stateManager.RebuildActiveMenu();
                else {
                    Debug.LogWarning("Failed to close door. Check if it's locked or destroyed.");
                }
            } else {
                success = door.TryOpenDoor();
                if (success) stateManager.RebuildActiveMenu();
                else {
                    Debug.LogWarning("Failed to open door. Check if it's locked or destroyed.");
                }
            }
        }

        private void UnlockDoor() {
            bool success;
            if (lockBehavior != null) {
                if(lockBehavior.IsLocked) {
                    success = lockBehavior.TryUnlock();
                    if (success) stateManager.RebuildActiveMenu();
                    else {
                        Debug.LogWarning("Failed to unlock door. It might already be unlocked.");
                    }
                } else {
                    success = lockBehavior.TryLock();
                    if (success) stateManager.RebuildActiveMenu();
                    else {
                        Debug.LogWarning("Failed to lock door. It might already be locked.");
                    }
                }
            }
        }

        private void DestroyDoor() {
            bool success;
            if (destroyBehavior != null) {
                success = door.TryDestroyDoor();
                if (success) {
                    Destroy(door.gameObject);
                } else {
                    Debug.LogWarning("Failed to destroy door.");
                }
            }
        }
    }
}