using UnityEngine;
using UnityEngine.Events;

public class CameraDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;//Looks for player tag if not assigned, but can be set directly for better performance

    [Header("Detection")]
    [SerializeField, Min(0f)] private float detectionRange;
    [SerializeField, Range(1f, 360f)] private float viewAngle;
    [SerializeField] private LayerMask environmentMask;
    [SerializeField] private LayerMask playerMask;

    [Header("Suspicion")]
    [SerializeField, Min(0f)] private float suspicionFillPerSecondAtClosest;
    [SerializeField, Min(0f)] private float suspicionFillPerSecondAtMaxRange;
    [SerializeField, Min(0f)] private float suspicionDecayPerSecond;

    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerDetected;
    [SerializeField] private UnityEvent onSuspicionStarted;
    [SerializeField] private UnityEvent onSuspicionCleared;

    private bool hasDetectedPlayer = false;
    private bool hadSuspicionLastFrame = false;
    private float suspicion = 0f;

    public float Suspicion => suspicion;
    public float DetectionRange => detectionRange;
    public float ViewAngle => viewAngle;
    public bool IsPlayerVisible { get; private set; }
    public bool HasDetectedPlayer => hasDetectedPlayer;
    public UnityEvent OnPlayerDetected => onPlayerDetected;
    public UnityEvent OnSuspicionStarted => onSuspicionStarted;
    public UnityEvent OnSuspicionCleared => onSuspicionCleared;

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void Update()
    {
        IsPlayerVisible = PerformDetectionCheck();

        if (IsPlayerVisible)
        {
            float normalizedDistance = Mathf.Clamp01(Vector2.Distance(transform.position, player.position) / detectionRange);
            float fillRate = Mathf.Lerp(suspicionFillPerSecondAtClosest, suspicionFillPerSecondAtMaxRange, normalizedDistance);
            suspicion = Mathf.Min(100f, suspicion + fillRate * Time.deltaTime);
        }
        else
        {
            suspicion = Mathf.Max(0f, suspicion - suspicionDecayPerSecond * Time.deltaTime);
        }

        bool hasSuspicion = suspicion > 0f;
        if (hasSuspicion && !hadSuspicionLastFrame)
        {
            onSuspicionStarted?.Invoke();
        }
        else if (!hasSuspicion && hadSuspicionLastFrame)
        {
            onSuspicionCleared?.Invoke();
        }

        hadSuspicionLastFrame = hasSuspicion;

        if (!hasDetectedPlayer && suspicion >= 100f)
        {
            hasDetectedPlayer = true;
            onPlayerDetected?.Invoke();
        }
    }

    public void ResetSuspicion()
    {
        suspicion = 0f;
        hasDetectedPlayer = false;
        hadSuspicionLastFrame = false;
    }

    private bool PerformDetectionCheck()
    {
        if (player == null)
        {
            return false;
        }

        Vector2 origin = transform.position;
        Vector2 toPlayer = (Vector2)(player.position - transform.position);
        float sqrDistance = toPlayer.sqrMagnitude;

        if (sqrDistance > detectionRange * detectionRange)
        {
            return false;//distance cheap compute check
        }

        float angleToPlayer = Vector2.Angle(-transform.up, toPlayer);
        if (angleToPlayer > viewAngle * 0.5f)
        {
            return false;//out of FOV
        }

        Vector2 direction = toPlayer.normalized;
        float distance = Mathf.Sqrt(sqrDistance);
        LayerMask losMask = environmentMask | playerMask;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, losMask);

        if (hit.collider == null)
        {
            return false;
        }

        return ((1 << hit.collider.gameObject.layer) & playerMask) != 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        float halfAngle = viewAngle * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0f, 0f, -halfAngle) * -transform.up;
        Vector3 rightDir = Quaternion.Euler(0f, 0f, halfAngle) * -transform.up;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, leftDir * detectionRange);
        Gizmos.DrawRay(transform.position, rightDir * detectionRange);

        if (player != null)
        {
            Gizmos.color = IsPlayerVisible ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
