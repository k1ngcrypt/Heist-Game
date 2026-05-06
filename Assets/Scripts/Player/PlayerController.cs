using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.1f;
    [SerializeField] private float gridSize = 1f;
    private bool isMoving = false;

    void Update()
    {
        // Only process input if we aren't already mid-animation
        if (!isMoving && Keyboard.current != null)
        {
            // Using a lambda for the delegate to match the new system's syntax
            System.Func<Key, bool> inputFunction = (key) => Keyboard.current[key].wasPressedThisFrame;

            if (inputFunction(Key.W)) StartCoroutine(Move(Vector2.up));
            else if (inputFunction(Key.A)) StartCoroutine(Move(Vector2.left));
            else if (inputFunction(Key.S)) StartCoroutine(Move(Vector2.down));
            else if (inputFunction(Key.D)) StartCoroutine(Move(Vector2.right));
        }
    }

    private IEnumerator Move(Vector2 direction)
    {
        isMoving = true;
        
        Vector2 startPosition = transform.position;
        Vector2 endPosition = startPosition + (direction * gridSize);
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / moveDuration;
            
            // Smoothly interpolate between the two tiles
            transform.position = Vector2.Lerp(startPosition, endPosition, percent);
            yield return null;
        }

        transform.position = endPosition;
        isMoving = false;
    }
}