using HeistGame.Door;
using HeistGame.Interactions;
using UnityEngine;

[CreateAssetMenu(fileName = "LockTool", menuName = "Heist Game/Loadout Items/Gadgets/Lock Door Tool")]
public class LockDoorTool : GadgetItem {
    [SerializeField] public int lockingToolID;
    private int combinedMask;

    protected override void ResetItemStats() {
        base.ResetItemStats();
        combinedMask = 1 << LayerMask.NameToLayer("Obstacle");
        if (combinedMask == null) combinedMask =  1 << LayerMask.NameToLayer("Pain");
    }

    private readonly Vector2[] moveDirections = new Vector2[] {
        Vector2.zero, new Vector2(0,0.5f), new Vector2(0,-0.5f), Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1, 1).normalized, new Vector2(-1, 1).normalized,
        new Vector2(1, -1).normalized, new Vector2(-1, -1).normalized
    };
    protected override async Awaitable<bool> OnExecute() {
        for (int i = 0; i < moveDirections.Length; i++) {
            Vector2 targetPos = (Vector2)player.transform.position + moveDirections[i];

            Collider2D hit = Physics2D.OverlapPoint(targetPos, combinedMask);

            if (hit != null) {
                DoorController door = hit.GetComponent<DoorController>();
                if (door != null) {
                    IDoorLockBehavior lockBehavior = door.GetComponent<IDoorLockBehavior>();
                    if (lockBehavior.IsLocked) {
                        bool success = lockBehavior.TryUnlock(lockingToolID);
                        if (success) {
                            InteractionStateManager stateManager;
                            foreach (Transform child in door.transform){
                                stateManager = child.GetComponent<InteractionStateManager>();
                                if (stateManager != null) {
                                    stateManager.RebuildActiveMenu(true);
                                    break;
                                }
                            }
                            await TurnManager.Instance.ProcessTicks(1);
                            return true; 
                        } else {
                            NotificationManager.Instance.SendNotification("Failed to unlock the obstacle.", Color.yellow);
                            return false;
                        }
                    } else {
                        NotificationManager.Instance.SendNotification("This door is unlocked!", Color.white);
                        return false;
                    }
                }
            }
        }
        NotificationManager.Instance.SendNotification("There is no object to use this tool on", Color.yellow);
        return false;
    }
}
