using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.1f;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private LayerMask wallLayer; // Assign "Unwalkable" layer here in Inspector
    [SerializeField, Tooltip("Assign the TurnManager in the scene to advance ticks after moves.")]
    private TurnManager turnManager;
    
    private bool isMoving = false;

    void Update() {
        if (!isMoving && Keyboard.current != null) {
            System.Func<Key, bool> inputFunction = (key) => Keyboard.current[key].isPressed;

            // Check each direction
            if (inputFunction(Key.W) || inputFunction(Key.UpArrow)) AttemptMove(Vector2.up);
            else if (inputFunction(Key.A) || inputFunction(Key.LeftArrow)) AttemptMove(Vector2.left);
            else if (inputFunction(Key.S) || inputFunction(Key.DownArrow)) AttemptMove(Vector2.down);
            else if (inputFunction(Key.D) || inputFunction(Key.RightArrow)) AttemptMove(Vector2.right);
            else if (inputFunction(Key.Z)) StartCoroutine(Rest());
        }
    }

    private IEnumerator Rest() {
        isMoving = true;
        Debug.Log("Resting...");
        float elapsedTime = 0f;
        if (turnManager != null) _ = turnManager.ProcessTicks(1);
        while (elapsedTime < 0.1f) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        isMoving = false;
    }

    private void AttemptMove(Vector2 direction) {
        Vector2 targetPos = (Vector2)transform.position + (direction * gridSize);
        if (!Physics2D.OverlapCircle(targetPos, 0.1f, wallLayer)) StartCoroutine(Move(direction));
        else Debug.Log("Wall in the way!");
    }

    private IEnumerator Move(Vector2 direction) {
        isMoving = true;
        Vector2 startPosition = transform.position;
        Vector2 endPosition = startPosition + (direction * gridSize);
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration) {
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / moveDuration;
            transform.position = Vector2.Lerp(startPosition, endPosition, percent);
            yield return null;
        }

        transform.position = endPosition;
        if (turnManager != null) _ = turnManager.ProcessTicks(1);
        isMoving = false;

    }
}