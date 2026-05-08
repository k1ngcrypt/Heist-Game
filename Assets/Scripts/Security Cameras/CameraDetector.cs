using UnityEngine;
using UnityEngine.Events;

public class CameraDetector : MonoBehaviour
{
    [Header("References")]
    private Transform player;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float detectionRange;
    [SerializeField, Range(1f, 360f)] private float viewAngle;
    [SerializeField] private LayerMask environmentMask;

    [Header("Suspicion")]
    [SerializeField, Min(0f)] private float suspicionFillPerTickAtClosest;
    [SerializeField, Min(0f)] private float suspicionFillPerTickAtMaxRange;
    [SerializeField, Min(0f)] private float suspicionDecayPerTick;

    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerDetected;
    [SerializeField] private UnityEvent onSuspicionStarted;
    [SerializeField] private UnityEvent onSuspicionCleared;

    private bool hasDetectedPlayer = false;
    private bool hadSuspicionLastFrame = false;
    private float suspicion = 0f;
    private ContactFilter2D losFilter;
    private readonly RaycastHit2D[] losHits = new RaycastHit2D[4];

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
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        InitializeLineOfSightFilter();
    }

    public void Tick()
    {
        if (!enabled)
        {
            return;
        }

        IsPlayerVisible = PerformDetectionCheck();

        if (IsPlayerVisible)
        {
            Debug.Log($"Player detected by {name} at distance {Vector2.Distance(transform.position, player.position):F2}");
            float normalizedDistance = Mathf.Clamp01(Vector2.Distance(transform.position, player.position) / detectionRange);
            float fillRate = Mathf.Lerp(suspicionFillPerTickAtClosest, suspicionFillPerTickAtMaxRange, normalizedDistance);
            suspicion = Mathf.Min(100f, suspicion + fillRate);
        }
        else
        {
            suspicion = Mathf.Max(0f, suspicion - suspicionDecayPerTick);
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
        Vector2 origin = transform.position;
        Vector2 toPlayer = (Vector2)(player.position - transform.position);
        float sqrDistance = toPlayer.sqrMagnitude;

        if (sqrDistance > detectionRange * detectionRange)
        {
            Debug.Log($"Player out of range for {name} (distance: {Mathf.Sqrt(sqrDistance):F2})");
            return false;//distance cheap compute check
        }

        float angleToPlayer = Vector2.Angle(-transform.up, toPlayer);
        if (angleToPlayer > viewAngle * 0.5f)
        {
            Debug.Log($"Player out of FOV for {name} (angle: {angleToPlayer:F2})");
            return false;//out of FOV
        }

        Vector2 direction = toPlayer.normalized;
        float distance = Mathf.Sqrt(sqrDistance);
        int hitCount = Physics2D.Raycast(origin, direction, losFilter, losHits, distance);

        if (hitCount > 0)
        {
            Debug.Log($"Line of sight blocked for {name} by {losHits[0].collider.name} (distance: {distance:F2})");
            return false;
        }

        return true;
    }

    private void InitializeLineOfSightFilter()
    {
        losFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = true
        };
        UpdateLineOfSightMask();
    }
    private void UpdateLineOfSightMask()
    {
        if (player == null)
        {
            return;
        }

        losFilter.layerMask = environmentMask;
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
