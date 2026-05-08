using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using HeistGame.Door;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private LayerMask wallLayer; 
    private Vector2 lastInputDirection;
    
    private bool isMoving = false;

    void Update() {
        if (!isMoving && Keyboard.current != null) {
            System.Func<Key, bool> inputFunction = (key) => Keyboard.current[key].isPressed;
            if (inputFunction(Key.W) || inputFunction(Key.UpArrow)) AttemptMove(Vector2.up);
            else if (inputFunction(Key.A) || inputFunction(Key.LeftArrow)) AttemptMove(Vector2.left);
            else if (inputFunction(Key.S) || inputFunction(Key.DownArrow)) AttemptMove(Vector2.down);
            else if (inputFunction(Key.D) || inputFunction(Key.RightArrow)) AttemptMove(Vector2.right);
            
            
            else if (inputFunction(Key.Z)) StartCoroutine(Rest());
            else if (Keyboard.current.eKey.wasPressedThisFrame) StartCoroutine(InteractWithObject());
            }
    }

    private IEnumerator Rest() {
        isMoving = true;
        Debug.Log("Resting...");
        if (TurnManager.Instance != null) _ = TurnManager.Instance.ProcessTicks(1);
        
        yield return new WaitForSeconds(0.1f);
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
        if (TurnManager.Instance != null) _ = TurnManager.Instance.ProcessTicks(1);

        yield return new WaitForSeconds(0.05f);
        isMoving = false;
    }

    private IEnumerator InteractWithObject() {
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        for (int i = 0; i < directions.Length; i++) {
            Vector2 targetPos = (Vector2)transform.position + (directions[i] * gridSize);
            int combinedMask = wallLayer | (1 << LayerMask.NameToLayer("Default"));
            Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.1f, combinedMask);

            if (hit != null) { //When there are more objects to interact with, more conditions will be added
                if (hit.CompareTag("Door")) StartCoroutine(InteractWithDoor(hit));
            } 
        }
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator InteractWithDoor(Collider2D door) {
        isMoving = true;
        DoorController doorScript = door.GetComponent<DoorController>();
        var openBehavior = door.GetComponent<IDoorOpenBehavior>();
        
        if (openBehavior != null) {
            bool success;
            if (openBehavior.IsOpen) {
                success = doorScript.TryCloseDoor();
                if (success) Debug.Log("Door closed!");
            } else {
                success = doorScript.TryOpenDoor();
                if (success) Debug.Log("Door opened!");
            }

            if (success) if (TurnManager.Instance != null) _ = TurnManager.Instance.ProcessTicks(1);
        }
        yield return new WaitForSeconds(0f);
        isMoving = false;
    }
}