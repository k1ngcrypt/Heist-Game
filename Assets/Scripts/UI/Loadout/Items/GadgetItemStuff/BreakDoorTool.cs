using UnityEngine;
using HeistGame.Interactions;
using HeistGame.Door;

[CreateAssetMenu(fileName = "BreakTool", menuName = "Heist Game/Loadout Items/Gadgets/Break Door Tool")]
public class BreakDoorTool : GadgetItem {
    [SerializeField] public int breakingToolID;
    private int combinedMask;

    public override void ResetItemStats() {
        base.ResetItemStats();
        combinedMask = 1 << LayerMask.NameToLayer("Obstacle") | 1 << LayerMask.NameToLayer("Pain");
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
                    IDoorDestroyBehavior destroyBehavior = door.GetComponent<IDoorDestroyBehavior>();
                    if (!destroyBehavior.IsDestroyed) {
                        bool success = destroyBehavior.TryDestroy(breakingToolID);
                        if (success) {
                            InteractionStateManager stateManager;
                            foreach (Transform child in door.transform){
                                stateManager = child.GetComponent<InteractionStateManager>();
                                if (stateManager != null) {
                                    stateManager.RebuildActiveMenu(true);
                                    break;
                                }
                            }
                            NotificationManager.Instance.SendMessage("The door has been destroyed!", Color.green);
                            await TurnManager.Instance.ProcessTicks(1);
                            return true; 
                        } else {
                            NotificationManager.Instance.SendNotification("Failed to destroy this door", Color.yellow);
                            return false;
                        }
                    } else {
                        NotificationManager.Instance.SendNotification("This obstacle is already destroyed", Color.white);
                        return false;
                    }
                }
            }
        }
        NotificationManager.Instance.SendNotification("There is no object to use this tool on", Color.yellow);
        return false;
    }
}
