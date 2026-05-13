using UnityEngine;
using UnityEngine.InputSystem;
using HeistGame.Door;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private LayerMask wallLayer; 
    
    private bool isMoving = false;
    private bool inVent = false;

    async void Update() {
        // Prevent starting new actions while one is in progress
        if (!isMoving && Keyboard.current != null) {
            System.Func<Key, bool> inputHeld = (key) => Keyboard.current[key].isPressed;

            if (inputHeld(Key.W) || inputHeld(Key.UpArrow)) await AttemptMove(Vector2.up);
            else if (inputHeld(Key.A) || inputHeld(Key.LeftArrow)) await AttemptMove(Vector2.left);
            else if (inputHeld(Key.S) || inputHeld(Key.DownArrow)) await AttemptMove(Vector2.down);
            else if (inputHeld(Key.D) || inputHeld(Key.RightArrow)) await AttemptMove(Vector2.right);
            
            else if (inputHeld(Key.Z)) await Rest();
            else if (Keyboard.current.eKey.wasPressedThisFrame) await InteractWithObject();
        }
    }

    private async Awaitable Rest() {
        isMoving = true;
        Debug.Log("Resting...");
        if (TurnManager.Instance != null) await TurnManager.Instance.ProcessTicks(1);
        
        await Awaitable.WaitForSecondsAsync(0.1f);
        isMoving = false;
    }

    private async Awaitable AttemptMove(Vector2 direction) {
        Vector2 targetPos = (Vector2)transform.position + (direction * gridSize);
        if (!Physics2D.OverlapCircle(targetPos, 0.1f, wallLayer)) await Move(direction);
        else Debug.Log("Wall in the way!");
    }

    private async Awaitable Move(Vector2 direction) {
        isMoving = true;
        Vector2 startPosition = transform.position;
        Vector2 endPosition = startPosition + (direction * gridSize);
        float elapsedTime = 0f;
        
        // Vents take twice as long to physically move through
        float currentMoveDuration = moveDuration * (inVent ? 1.5f : 1);

        while (elapsedTime < currentMoveDuration) {
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / currentMoveDuration;
            transform.position = Vector2.Lerp(startPosition, endPosition, percent);
            await Awaitable.EndOfFrameAsync(); 
        }

        transform.position = endPosition;
        
        await TurnManager.Instance.ProcessTicks(inVent ? 2 : 1);
        await Awaitable.WaitForSecondsAsync(0.1f);
        isMoving = false;
    }

    private async Awaitable InteractWithObject() {
        // Directions check including current tile (Vector2.zero)
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right, Vector2.zero };

        for (int i = 0; i < directions.Length; i++) {
            Vector2 targetPos = (Vector2)transform.position + (directions[i] * gridSize);
            int combinedMask = wallLayer | (1 << LayerMask.NameToLayer("Default"));
            Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.1f, combinedMask);

            if (hit != null) {
                if (hit.CompareTag("Door")) { await ProcessInteraction(hit, 0); break; }
                else if (hit.CompareTag("Vent")) { await ProcessInteraction(hit, 1); break; }
                else if (hit.CompareTag("Stair")) { await ProcessInteraction(hit, 2); break; }
            } 
        }
    }

    private async Awaitable ProcessInteraction(Collider2D obj, short doorType) {
        isMoving = true;
        DoorController doorScript = obj.GetComponent<DoorController>();
        var openBehavior = obj.GetComponent<IDoorOpenBehavior>();
        
        if (openBehavior != null && doorScript != null) {
            bool success;

            if (doorType == 1) inVent = !inVent; 
            else if (doorType == 2) {
                doorScript.TryOpenDoor();
                if (TurnManager.Instance != null) await TurnManager.Instance.ProcessTicks(4);
                isMoving = false;
                return;
            }
            if (openBehavior.IsOpen) {
                success = doorScript.TryCloseDoor();
                if (success) Debug.Log("Object closed!");
            } else {
                success = doorScript.TryOpenDoor();
                if (success) Debug.Log("Object opened!");
            }

            if (success && TurnManager.Instance != null) {
                int ticks = (doorType == 0) ? 1 : 3; // Doors take 1 tick, Vents take 3 ticks, Stairs take 4 ticks
                await TurnManager.Instance.ProcessTicks(ticks);
            }
        }
        
        isMoving = false;
    }
}