using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target; // Drag player here
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10); // Keep camera at a distance

    private Vector3 velocity = Vector3.zero;
    void Start() {
        if (target == null) {
            if (Map.Player != null) target = Map.Player.transform;
            else target = GameObject.FindWithTag("Player").transform;
        }
    }

    // LateUpdate is better for cameras because it runs after the player has moved
    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;
            
            // Smoothly move the camera to the target position
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}