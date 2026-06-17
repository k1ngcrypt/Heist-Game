using Guards;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private AwarenessManager awarenessManager;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap baseTilemap;
    private Animator animator;
    private PlayerStats playerStats;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int DirectionHash = Animator.StringToHash("Direction");
    [SerializeField] private LineRenderer weaponVisuals;
    
    private bool isMoving = false;
    public bool inVent = false;

    private const int ventMoveTicks = 2;
    private const float ventMoveDurationMultiplier = 1.5f, restDuration = 0.1f, interactionDuration = 0.1f;

    private readonly Vector2[] moveDirections = new Vector2[] {
        Vector2.zero, new(0,0.5f), new(0,-0.5f), Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1, 1).normalized, new Vector2(-1, 1).normalized,
        new Vector2(1, -1).normalized, new Vector2(-1, -1).normalized
    };
    private int combinedMask;
    void Awake() {
        Map.SetPlayer(gameObject);
    }

    void Start() { 
        Map.SetPlayer(gameObject);
        if (cameraManager==null) cameraManager = FindAnyObjectByType<CameraManager>();
        combinedMask = wallLayer | (1 << LayerMask.NameToLayer("Pain")); 
        animator = GetComponent<Animator>();
        playerStats = GetComponent<PlayerStats>();
    }
    async void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { SceneUIManager.Instance.TogglePause(); return; }

        // Prevent starting new actions while one is in progress
        if (!isMoving && Keyboard.current != null && !SceneUIManager.Instance.IsPaused())
        {
            static bool inputHeld(Key key) => Keyboard.current[key].isPressed;

            if (inputHeld(Key.W) || inputHeld(Key.UpArrow))
            {
                animator.SetInteger(DirectionHash, 1);
                await AttemptMove(Vector2.up);
            }
            else if (inputHeld(Key.A) || inputHeld(Key.LeftArrow))
            {
                animator.SetInteger(DirectionHash, 0);
                await AttemptMove(Vector2.left);
            }
            else if (inputHeld(Key.S) || inputHeld(Key.DownArrow))
            {
                animator.SetInteger(DirectionHash, 3);
                await AttemptMove(Vector2.down);
            }
            else if (inputHeld(Key.D) || inputHeld(Key.RightArrow))
            {
                animator.SetInteger(DirectionHash, 2);
                await AttemptMove(Vector2.right);
            }

            else if (inputHeld(Key.Z)) await Rest();
            else if (Keyboard.current.eKey.wasPressedThisFrame) await InteractWithObject();
            else if (TryGetPressedNumber(out int pressedNumber)) await TryButtonPress(pressedNumber);
            else if (Keyboard.current.fKey.wasPressedThisFrame) await UseGadget();
            else if (Keyboard.current.rKey.wasPressedThisFrame) await ReloadWeapon();
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject()) await Shoot();
        }
    }

    private bool TryGetPressedNumber(out int pressedNumber)
    {
        pressedNumber = -1;
        if (Keyboard.current == null) return false;
        pressedNumber = Keyboard.current switch
        {
            var k when k.digit1Key.wasPressedThisFrame => 1,
            var k when k.digit2Key.wasPressedThisFrame => 2,
            var k when k.digit3Key.wasPressedThisFrame => 3,
            var k when k.digit4Key.wasPressedThisFrame => 4,
            var k when k.digit5Key.wasPressedThisFrame => 5,
            _ => -1
        };

        return pressedNumber != -1;
    }

    private async Awaitable Rest()
    {
        isMoving = true;
        if (TurnManager.Instance != null) await TurnManager.Instance.ProcessTicks(1);

        await Awaitable.WaitForSecondsAsync(restDuration);
        isMoving = false;
    }

    private async Awaitable AttemptMove(Vector2 direction)
    {
        Vector2 targetPos = (Vector2)transform.position + (direction * gridSize);
        if (Map.IsInitialized) {
            if (Map.IsNull(Map.AlignObjectToGrid(targetPos))&&!Physics2D.OverlapCircle(targetPos, 0.1f, wallLayer)) await Move(direction);
        } else if (!Physics2D.OverlapCircle(targetPos, 0.1f, wallLayer)) await Move(direction);
    }

    private async Awaitable Move(Vector2 direction)
    {
        isMoving = true;
        Vector2 startPosition = transform.position;
        Vector2 endPosition = startPosition + (direction * gridSize);
        float elapsedTime = 0f;

        // Vents take twice as long to physically move through
        float currentMoveDuration = moveDuration * (inVent ? ventMoveDurationMultiplier : 1);
        animator.SetBool(IsWalkingHash, true);

        while (elapsedTime < currentMoveDuration)
        {
            if (SceneUIManager.Instance.IsPaused() && SceneUIManager.Instance.GetPauseSignal() != null) await SceneUIManager.Instance.GetPauseSignal().Awaitable;
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / currentMoveDuration;
            transform.position = Vector2.Lerp(startPosition, endPosition, percent);
            await Awaitable.EndOfFrameAsync();
        }

        animator.SetBool(IsWalkingHash, false);
        transform.position = Map.AlignToObjectPos(endPosition);
        int fall = 0;
        while (CheckGround()) {
            await Awaitable.WaitForSecondsAsync(0.1f);
            fall++;
        }
        if (fall>0) playerStats.TakeDamage(fall*5);
        awarenessManager.MakeSound(endPosition, 0.8f); // Make noise on move
        await TurnManager.Instance.ProcessTicks(inVent ? ventMoveTicks : 1);
        await Awaitable.WaitForSecondsAsync(interactionDuration);
        //GetComponent<PlayerStats>().TakeDamage(10);
        isMoving = false;
    }

    private async Awaitable InteractWithObject()
    {
        isMoving = true;
        for (int i = 0; i < moveDirections.Length; i++)
        {
            Vector2 targetPos = (Vector2)transform.position + (moveDirections[i] * gridSize);
            Collider2D hit = Physics2D.OverlapPoint(targetPos, combinedMask);

            if (hit != null)
            {
                InteractionOverlay interactOverlay = hit.GetComponentInChildren<InteractionOverlay>();
                if (interactOverlay == null) interactOverlay = hit.GetComponent<InteractionOverlay>();
                if (interactOverlay != null){
                    interactOverlay.ToggleMenuStatus();
                    break;
                }
            }
        }
        isMoving = false;
    }

    private async Awaitable TryButtonPress(int number)
    {
        isMoving = true;
        for (int i = 0; i < moveDirections.Length; i++)
        {
            Vector2 targetPos = (Vector2)transform.position + (moveDirections[i] * gridSize);

            Collider2D hit = Physics2D.OverlapPoint(targetPos, combinedMask);

            if (hit != null)
            {
                InteractArea interactArea = hit.GetComponent<InteractArea>();
                InteractionOverlay overlay = null;
                foreach (Transform child in hit.transform)
                {
                    if (interactArea != null) break;
                    interactArea = child.GetComponent<InteractArea>();
                }
                if (interactArea != null) overlay = interactArea.GetComponentInChildren<InteractionOverlay>();
                if (interactArea != null && overlay != null)
                {
                    if (!overlay.isMenuOpen) { continue; }
                    List<InteractBtnTemplate> activeButtons = interactArea.GetActiveButtons();
                    int targetIndex = number - 1;
                    if (targetIndex >= 0 && targetIndex < activeButtons.Count)
                    {
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
    private async Awaitable UseGadget() {
        isMoving = true;

        LoadoutItems item = InventoryManager.Instance.ReturnEquipItem();
        if (item != null) await ((GadgetItem)item).TryExecute();

        isMoving = false;
    }

    private bool CheckGround()
    {
        if (floorTilemap == null || baseTilemap == null || !Map.IsInitialized) return false;
        Vector3Int pos = floorTilemap.WorldToCell(transform.position);
        if (!floorTilemap.HasTile(pos) && !baseTilemap.HasTile(pos))
        {
            int layer = Map.CurrentLayer();
            if (layer == 0) return false;
            Vector3Int lPos = pos + new Vector3Int((int)(Map.layerLocations[layer - 1].x - Map.layerLocations[layer].x), (int)(Map.layerLocations[layer - 1].y - Map.layerLocations[layer].y), 0);
            if (Map.IsNull(lPos))
            {
                transform.position = lPos + new Vector3(0.5f, 0.5f, 0);
                return true;
            }
        }
        return false;
    }
    private async Awaitable Shoot() {
        isMoving = true;

        WeaponItem weapon = InventoryManager.Instance.ReturnEquipWeapon() as WeaponItem;
        if (weapon.itemTitle == "Empty"){
            isMoving = false;
            return; 
        }
        else if (weapon.currentAmmo <= 0){
            NotificationManager.Instance.SendNotification("You have no ammo to shoot!", Color.yellow);
            isMoving = false;
            return;
        }
        GuardStateManager[] allGuards = FindObjectsByType<GuardStateManager>();
        GuardStateManager closestTarget = null;
        
        Vector3 mouseWorldPos = cameraManager.GetCurrentCamera().ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 direction = (mouseWorldPos - transform.position).normalized;
        float maxShootDistance = weapon.range;
        Vector3 finalVisualTargetPosition = transform.position + (Vector3)(direction * maxShootDistance);

        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, direction, weapon.range, ~(1 << LayerMask.NameToLayer("Default")));
        
        if (wallHit.collider != null) {
            maxShootDistance = wallHit.distance;
            finalVisualTargetPosition = wallHit.point;
        }

        float closestGuardDistance = maxShootDistance;

        foreach (GuardStateManager guard in allGuards)
        {
            // Extra tag validation safety rule
            if (!guard.CompareTag("Guard")) continue;

            Vector2 guardPos = guard.transform.position;
            Vector2 toGuard = guardPos - (Vector2)transform.position;


            if (Vector2.Dot(toGuard.normalized, direction.normalized) > 0.95f) {
                float distance = toGuard.magnitude;

                if (distance <= closestGuardDistance) {
                    closestGuardDistance = distance;
                    closestTarget = guard;
                    finalVisualTargetPosition = guard.transform.position;
                }
            }
        }
        if (closestTarget != null) {
            Debug.Log($"Direct hit confirmed on: {closestTarget.gameObject.name} without a collider!");
            AwarenessManager.Instance.MakeSound(transform.position, weapon.soundDistance);
            closestTarget.GetComponent<HealthManager>().TakeDamage(weapon.damageValue);
        } else {
            Debug.Log("Shot missed or hit a structural wall.");
        }
        weapon.currentAmmo--;
        VisualEffects(finalVisualTargetPosition);
        await TurnManager.Instance.ProcessTicks(1);
        isMoving = false;
    }

    private async void VisualEffects(Vector3 hit) {
        if (weaponVisuals != null) {
            weaponVisuals.SetPosition(0, transform.position);
            weaponVisuals.SetPosition(1, hit);
            weaponVisuals.enabled = true;
            await Awaitable.WaitForSecondsAsync(0.1f);
            if (weaponVisuals != null) weaponVisuals.enabled = false;
        }
    }

    private async Awaitable ReloadWeapon() {
        isMoving = true;
        WeaponItem weapon = InventoryManager.Instance.ReturnEquipWeapon() as WeaponItem;
        if (weapon.itemTitle == "Empty") {isMoving = false; return;}

        weapon.ReloadWeapon();
        await TurnManager.Instance.ProcessTicks(1);
        isMoving = false;
    }
}