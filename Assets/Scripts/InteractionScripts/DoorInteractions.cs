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
            if (openBehavior.IsOpen) {
                if (door.TryCloseDoor()) {
                    stateManager.RebuildActiveMenu();
                    await TurnManager.Instance.ProcessTicks(ticksUsedToOpen);
                }
            } else {
                if (door.TryOpenDoor()) {
                    stateManager.RebuildActiveMenu();
                    await TurnManager.Instance.ProcessTicks(ticksUsedToOpen);
                }
            }
        }

        private async void UnlockDoor() {
            bool success;
            //Get Eqqipped Tool Id Here When Made
            if (lockBehavior != null) {
                if(lockBehavior.IsLocked) {
                    if (lockBehavior is Lockable) {
                        LoadoutItems item = InventoryManager.Instance.ReturnEquipItem();
                        if (item == null) {
                            NotificationManager.Instance.SendNotification("You need to hold the right tool to unlock the door", Color.yellow);
                        }
                        else if (item is not LockDoorTool) {
                            NotificationManager.Instance.SendNotification("You can not unlock a door with that tool.", Color.yellow);
                        }
                        success = lockBehavior.TryUnlock(((LockDoorTool)item).lockingToolID);
                    } else success = lockBehavior.TryUnlock(0);
                    if (success) {
                        stateManager.RebuildActiveMenu();
                        await TurnManager.Instance.ProcessTicks(ticksUsedToLock);
                    } else {
                        NotificationManager.Instance.SendNotification($"Failed to unlock {nameOfDoor}. You might need a key or the right tool.", Color.yellow);
                    }
                } else {
                    success = lockBehavior.TryLock();
                    if (success) {
                        stateManager.RebuildActiveMenu();
                        await TurnManager.Instance.ProcessTicks(ticksUsedToLock);
                    } else {
                        NotificationManager.Instance.SendNotification($"Failed to lock {nameOfDoor}. You might need a key or the right tool.", Color.yellow);
                    }
                }
            }
        }

        private async void DestroyDoor() {
            bool success;
            if (destroyBehavior != null) {
                if (destroyBehavior is Destructible) {
                    LoadoutItems item = InventoryManager.Instance.ReturnEquipItem();
                    if (item == null) {
                        NotificationManager.Instance.SendNotification("You need to hold the right tool to break down the door", Color.yellow);
                    }
                    else if (item is not BreakDoorTool) {
                        NotificationManager.Instance.SendNotification("You can not destroy a door with that tool.", Color.yellow);
                    }
                    success = destroyBehavior.TryDestroy(((BreakDoorTool)item).breakingToolID);
                } else {
                    NotificationManager.Instance.SendNotification("You can not break down this door", Color.yellow);
                    return;
                }
                if (success) {
                    await TurnManager.Instance.ProcessTicks(ticksUsedToDestroy);
                } else {
                    NotificationManager.Instance.SendNotification($"Failed to break {nameOfDoor}. You need the right tool.", Color.yellow);
                }
            }
        }
    }
}