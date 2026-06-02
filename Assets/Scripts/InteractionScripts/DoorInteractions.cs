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

        [SerializeField] private string nameOfDoor;
        [SerializeField] private bool openable;
        [SerializeField] private bool lockable;
        [SerializeField] private bool destroyable;
        [SerializeField] private int ticksUsedToOpen = 1;
        [SerializeField] private int ticksUsedToLock = 1;
        [SerializeField] private int ticksUsedToDestroy = 1;

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
                    string openText = openBehavior.IsOpen? $"Close {nameOfDoor}" : $"Open {nameOfDoor}";
                    
                    InteractBtnTemplate openBtn = new InteractBtnTemplate();
                    openBtn.text = openText;
                    openBtn.onClick.AddListener(InteractWithDoor);
                    myButtons.Add(openBtn);
                }
            }

            if (lockable) {
                if (lockBehavior != null) {
                string lockText = lockBehavior.IsLocked? $"Unlock {nameOfDoor}" : $"Lock {nameOfDoor}";

                    InteractBtnTemplate lockBtn = new InteractBtnTemplate();
                    lockBtn.text = lockText;
                    lockBtn.onClick.AddListener(UnlockDoor);
                    myButtons.Add(lockBtn);
                }
            }

            if (destroyable) {
                InteractBtnTemplate destroyBtn = new InteractBtnTemplate();
                destroyBtn.text = $"Break {nameOfDoor}";
                destroyBtn.onClick.AddListener(DestroyDoor);
                myButtons.Add(destroyBtn);
            }
            return myButtons;
        }

        private async void InteractWithDoor() {
            bool success;
            if (openBehavior.IsOpen) {
                success = door.TryCloseDoor();
                if (success) {
                    stateManager.RebuildActiveMenu();
                    await TurnManager.Instance.ProcessTicks(ticksUsedToOpen);
                } else {
                    Debug.LogWarning($"Failed to close {nameOfDoor}. Check if it's locked or destroyed.");
                }
            } else {
                success = door.TryOpenDoor();
                if (success) {
                    stateManager.RebuildActiveMenu();
                    await TurnManager.Instance.ProcessTicks(ticksUsedToOpen);
                } else {
                    Debug.LogWarning($"Failed to open {nameOfDoor}. Check if it's locked or destroyed.");
                }
            }
            await Awaitable.EndOfFrameAsync();
        }

        private async void UnlockDoor() {
            bool success;
            if (lockBehavior != null) {
                if(lockBehavior.IsLocked) {
                    success = lockBehavior.TryUnlock();
                    if (success) {
                        stateManager.RebuildActiveMenu();
                        await TurnManager.Instance.ProcessTicks(ticksUsedToLock);
                    }else {
                        Debug.LogWarning($"Failed to unlock {nameOfDoor}. It might already be unlocked.");
                    }
                } else {
                    success = lockBehavior.TryLock();
                    if (success) {
                        stateManager.RebuildActiveMenu();
                        await TurnManager.Instance.ProcessTicks(ticksUsedToLock);
                    } else {
                        Debug.LogWarning($"Failed to lock {nameOfDoor}. It might already be locked.");
                    }
                }
            }
            await Awaitable.EndOfFrameAsync();
        }

        private async void DestroyDoor() {
            bool success;
            if (destroyBehavior != null) {
                success = door.TryDestroyDoor();
                if (success) {
                    await TurnManager.Instance.ProcessTicks(ticksUsedToDestroy);
                    Destroy(door.gameObject);
                } else {
                    Debug.LogWarning($"Failed to destroy {nameOfDoor}.");
                }
            }
            await Awaitable.EndOfFrameAsync();
        }
    }
}