using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.1f;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private LayerMask wallLayer; // Assign "Unwalkable" layer here in Inspector
    
    private bool isMoving = false;

    void Update() {
        if (!isMoving && Keyboard.current != null) {
            System.Func<Key, bool> inputFunction = (key) => Keyboard.current[key].wasPressedThisFrame;

            // Check each direction
            if (inputFunction(Key.W)) AttemptMove(Vector2.up);
            else if (inputFunction(Key.A)) AttemptMove(Vector2.left);
            else if (inputFunction(Key.S)) AttemptMove(Vector2.down);
            else if (inputFunction(Key.D)) AttemptMove(Vector2.right);
        }
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
        //TurnManager.Instance.ProcessTicks(1); // Process 1 tick after moving
        isMoving = false;

    }
}