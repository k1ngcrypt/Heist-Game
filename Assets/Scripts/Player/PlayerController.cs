using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using HeistGame.Door;
using HeistGame.Objectives;
using UnityEditor.Experimental.GraphView;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
public class PlayerController : MonoBehaviour {
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private AwarenessManager awarenessManager;
    private PlayerStats playerStats;
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap baseTilemap;
    
    private bool isMoving = false;
    private bool isPaused = false;
    private TaskCompletionSource<bool> _pauseSignal;
    public bool inVent = false;

    private const int doorWaitTicks = 1, ventWaitTicks = 3, stairWaitTicks = 4, ventMoveTicks = 2;
    private const float ventMoveDurationMultiplier = 1.5f, restDuration = 0.1f, interactionDuration = 0.1f;

    private readonly Vector2[] moveDirections = new Vector2[] {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1, 1).normalized, new Vector2(-1, 1).normalized,
        new Vector2(1, -1).normalized, new Vector2(-1, -1).normalized, Vector2.zero
    };
    private int combinedMask;

    public void SetPaused(bool paused) {
        isPaused = paused;
        if (!isPaused) {
            _pauseSignal?.TrySetResult(true);
        } else {
            _pauseSignal = new TaskCompletionSource<bool>();
        }
    }

    void Start() { 
        Map.SetPlayer(gameObject); combinedMask  = wallLayer | (1 << LayerMask.NameToLayer("Default")); 
        playerStats = GetComponent<PlayerStats>();
    }
    async void Update() {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { SetPaused(!isPaused); return; }
        
        // Prevent starting new actions while one is in progress
        if (!isMoving && Keyboard.current != null && !isPaused) {
            System.Func<Key, bool> inputHeld = (key) => Keyboard.current[key].isPressed;

            if (inputHeld(Key.W) || inputHeld(Key.UpArrow)) await AttemptMove(Vector2.up);
            else if (inputHeld(Key.A) || inputHeld(Key.LeftArrow)) await AttemptMove(Vector2.left);
            else if (inputHeld(Key.S) || inputHeld(Key.DownArrow)) await AttemptMove(Vector2.down);
            else if (inputHeld(Key.D) || inputHeld(Key.RightArrow)) await AttemptMove(Vector2.right);
            
            else if (inputHeld(Key.Z)) await Rest();
            else if (Keyboard.current.eKey.wasPressedThisFrame) await InteractWithObject();
            else if (TryGetPressedNumber(out int pressedNumber)) await TryButtonPress(pressedNumber);
        }
    }

    private bool TryGetPressedNumber(out int pressedNumber) {
        pressedNumber = -1;
        if (Keyboard.current == null) return false;
        pressedNumber = Keyboard.current switch {
            var k when k.digit1Key.wasPressedThisFrame => 1,
            var k when k.digit2Key.wasPressedThisFrame => 2,
            var k when k.digit3Key.wasPressedThisFrame => 3,
            var k when k.digit4Key.wasPressedThisFrame => 4,
            var k when k.digit5Key.wasPressedThisFrame => 5,
            _ => -1
        };
        
        return pressedNumber != -1;
    }

    private async Awaitable Rest() {
        isMoving = true;
        if (TurnManager.Instance != null) await TurnManager.Instance.ProcessTicks(1);
        
        await Awaitable.WaitForSecondsAsync(restDuration);
        isMoving = false;
    }

    private async Awaitable AttemptMove(Vector2 direction) {
        Vector2 targetPos = (Vector2)transform.position + (direction * gridSize);
        if (Map.IsInitialized) {
            if (Map.IsNull(new Vector3Int((int)Math.Round(targetPos.x-0.5f),(int)Math.Round(targetPos.y-0.5f)))) await Move(direction);
        } else if (!Physics2D.OverlapCircle(targetPos, 0.1f, wallLayer)) await Move(direction);
    }

    private async Awaitable Move(Vector2 direction) {
        isMoving = true;
        Vector2 startPosition = transform.position;
        Vector2 endPosition = startPosition + (direction * gridSize);
        float elapsedTime = 0f;
        
        // Vents take twice as long to physically move through
        float currentMoveDuration = moveDuration * (inVent ? ventMoveDurationMultiplier : 1);

        while (elapsedTime < currentMoveDuration) {
            if (isPaused) await _pauseSignal.Task;
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / currentMoveDuration;
            transform.position = Vector2.Lerp(startPosition, endPosition, percent);
            await Awaitable.EndOfFrameAsync(); 
        }

        transform.position = endPosition;
        CheckGround();
        awarenessManager.MakeSound(endPosition, 0.8f); // Make noise on move
        await TurnManager.Instance.ProcessTicks(inVent ? ventMoveTicks : 1);
        await Awaitable.WaitForSecondsAsync(interactionDuration);
        //playerStats.additionalArmour += 5;
        //playerStats.takeDamage(10);
        isMoving = false;
    }

    private async Awaitable InteractWithObject() {
        isMoving = true;
        for (int i = 0; i < moveDirections.Length; i++) {
            Vector2 targetPos = (Vector2)transform.position + (moveDirections[i] * gridSize);
            Collider2D hit = Physics2D.OverlapPoint(targetPos, combinedMask);

            if (hit != null) {
                InteractionOverlay interactOverlay = hit.GetComponentInChildren<InteractionOverlay>();
                if (interactOverlay != null) {
                    interactOverlay.ToggleMenuStatus();
                    break;
                }
            } 
        }
        isMoving = false;
    }

    private async Awaitable ObjectiveCheck(Collider2D obj) {
        isMoving = true;
        var trigger = obj.GetComponent<ObjectiveTrigger>();
        if (trigger != null) {
            trigger.TriggerProgress();
            await Awaitable.WaitForSecondsAsync(interactionDuration);
        }
        isMoving = false;
    }

    private async Awaitable TryButtonPress(int number) {
        isMoving = true;
        for (int i = 0; i < moveDirections.Length; i++) {
            Vector2 targetPos = (Vector2)transform.position + (moveDirections[i] * gridSize);
            
            Collider2D hit = Physics2D.OverlapPoint(targetPos, combinedMask);

            if (hit != null) {
                InteractArea interactArea = null;
                InteractionOverlay overlay = null;
                foreach (Transform child in hit.transform) {
                    interactArea = child.GetComponent<InteractArea>();
                    if (interactArea != null) break;
                }
                if (interactArea != null) overlay = interactArea.GetComponentInChildren<InteractionOverlay>();
                if (interactArea != null && overlay != null) {
                    if (!overlay.isMenuOpen) { continue; }
                    List<InteractBtnTemplate> activeButtons = interactArea.GetActiveButtons();
                    int targetIndex = number - 1;
                    if (targetIndex >= 0 && targetIndex < activeButtons.Count) {
                        activeButtons[targetIndex].onClick.Invoke();
                        await Awaitable.WaitForSecondsAsync(interactionDuration);
                        isMoving = false;
                        return;
                    } 
                    else NotificationManager.Instance.SendNotification($"No button assigned to {number} in this menu.", Color.yellow);
                    isMoving = false;
                    return;
                }
            } 
        }
        isMoving = false;
    }

    private void CheckGround() {
        if (floorTilemap == null || baseTilemap == null || !Map.IsInitialized) return;
        Vector3Int pos = floorTilemap.WorldToCell(transform.position);
        if (!floorTilemap.HasTile(pos) && !baseTilemap.HasTile(pos)) {
            int layer = Map.CurrentLayer();
            if (layer==0) return;
            Vector3Int lPos = pos + new Vector3Int((int)(Map.layerLocations[layer-1].x - Map.layerLocations[layer].x), (int)(Map.layerLocations[layer-1].y - Map.layerLocations[layer].y), 0);
            if (Map.IsNull(lPos)) transform.position = lPos + new Vector3(0.5f, 0.5f, 0);
        }
    }
}