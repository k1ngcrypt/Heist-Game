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
    private Vector2 lastSeenPosition;
    private bool hasLastSeenPosition;
    private ContactFilter2D losFilter;
    private readonly RaycastHit2D[] losHits = new RaycastHit2D[4];

    public float Suspicion => suspicion;
    public float DetectionRange => detectionRange;
    public float ViewAngle => viewAngle;
    public bool IsPlayerVisible { get; private set; }
    public bool HasDetectedPlayer => hasDetectedPlayer;
    public Vector2 LastSeenPosition => lastSeenPosition;
    public bool HasLastSeenPosition => hasLastSeenPosition;
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

    private void OnEnable()
    {
        AwarenessManager.Instance?.RegisterCamera(this);
    }

    private void OnDisable()
    {
        AwarenessManager.Instance?.UnregisterCamera(this);
    }

    public void Tick()
    {
        if (!enabled)
        {
            return;
        }

        IsPlayerVisible = DetectionUtils.IsDetected(
            transform.position,
            -transform.up,
            player,
            detectionRange,
            viewAngle,
            losFilter,
            losHits,
            0f
        );

        if (IsPlayerVisible)
        {
            if (player != null)
            {
                lastSeenPosition = player.position;
                hasLastSeenPosition = true;
            }

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
        hasLastSeenPosition = false;
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
