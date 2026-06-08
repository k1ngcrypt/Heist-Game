using System.Collections.Generic;
using System.Linq;
using Guards;
using UnityEngine;
using UnityEngine.Events;

public enum AwarenessLevel
{
    Calm,
    Suspicious,
    Alert,
    Lockdown
}

public class AwarenessManager : MonoBehaviour, ITurnActor
{
    [Header("Awareness")]
    [SerializeField, Min(0f)] private float maxAwareness = 1000f;
    [SerializeField, Min(0f)] private float awarenessDecayPerTick = 0.1f;
    [SerializeField, Min(0f)] private float guardSuspicionWeight = 1f;

    [Header("Levels")]
    [SerializeField, Range(0f, 1000f)] private float suspiciousThreshold = 75f;
    [SerializeField, Range(0f, 1000f)] private float alertThreshold = 100f;
    [SerializeField, Range(0f, 1000f)] private float lockdownThreshold = 200f;

    [Header("Dispatch")]
    [SerializeField, Min(0f)] private float cameraSuspicionBoost = 10f;
    [SerializeField, Min(0f)] private float cameraDetectionBoost = 25f;
    [SerializeField, Min(0)] private int dispatchCooldownTicks = 2;
    [SerializeField] private TurnManager turnManager;

    private readonly List<GuardStateManager> guards = new();
    private readonly Dictionary<CameraDetector, UnityAction> cameraSuspicionHandlers = new();
    private readonly Dictionary<CameraDetector, UnityAction> cameraDetectionHandlers = new();
    public float awareness { get; private set; }
    public List<Transform> anomalies { get; private set; } = new List<Transform>();
    private AwarenessLevel currentLevel;
    private int tickCount;
    private int lastDispatchTick = -1;
    private bool playerSeenThisTick;
    private bool hasLastKnownPlayerPosition;
    private Vector2 lastKnownPlayerPosition;

    public static AwarenessManager Instance { get; private set; }
    public AwarenessLevel CurrentLevel => currentLevel;
    public int TickDebt { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (turnManager == null)
        {
            turnManager = TurnManager.Instance ?? FindAnyObjectByType<TurnManager>();
        }

        if (turnManager != null)
        {
            turnManager.Register(this);
        }

        RegisterExistingGuards();
        RegisterExistingCameras();
        UpdateAwarenessLevel();
    }

    private void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.Unregister(this);
        }

        UnregisterAllCameras();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void RegisterExistingGuards()
    {
        guards.Clear();
        foreach (GuardStateManager guard in FindObjectsByType<GuardStateManager>())
        {
            RegisterGuard(guard);
        }
    }

    private void RegisterExistingCameras()
    {
        foreach (CameraDetector detector in FindObjectsByType<CameraDetector>())
        {
            RegisterCamera(detector);
        }
    }

    public void RegisterGuard(GuardStateManager guard)
    {
        if (guard == null || guards.Contains(guard))
        {
            return;
        }

        guards.Add(guard);
    }

    public void UnregisterGuard(GuardStateManager guard)
    {
        if (guard == null)
        {
            return;
        }

        guards.Remove(guard);
    }

    public void RegisterCamera(CameraDetector detector)
    {
        if (detector == null || cameraSuspicionHandlers.ContainsKey(detector))
        {
            return;
        }

        void suspicionHandler() => HandleCameraSuspicion(detector);
        void detectionHandler() => HandleCameraDetection(detector);

        cameraSuspicionHandlers.Add(detector, suspicionHandler);
        cameraDetectionHandlers.Add(detector, detectionHandler);

        detector.OnSuspicionStarted.AddListener(suspicionHandler);
        detector.OnPlayerDetected.AddListener(detectionHandler);
    }

    public void UnregisterCamera(CameraDetector detector)
    {
        if (detector == null)
        {
            return;
        }

        if (cameraSuspicionHandlers.TryGetValue(detector, out UnityAction suspicionHandler))
        {
            detector.OnSuspicionStarted.RemoveListener(suspicionHandler);
            cameraSuspicionHandlers.Remove(detector);
        }

        if (cameraDetectionHandlers.TryGetValue(detector, out UnityAction detectionHandler))
        {
            detector.OnPlayerDetected.RemoveListener(detectionHandler);
            cameraDetectionHandlers.Remove(detector);
        }

    }

    private void UnregisterAllCameras()
    {
        foreach (CameraDetector detector in new List<CameraDetector>(cameraSuspicionHandlers.Keys))
        {
            UnregisterCamera(detector);
        }
    }

    public void RegisterAnomaly(Transform anomaly)
    {
        if (anomaly == null || anomalies.Contains(anomaly))
        {
            return;
        }
        anomalies.Add(anomaly);
    }

    public void UnregisterAnomaly(Transform anomaly)
    {
        if (anomaly == null || !anomalies.Contains(anomaly))
        {
            return;
        }
        anomalies.Remove(anomaly);
    }

    public async Awaitable OnTick()
    {
        if (!enabled)
        {
            return;
        }

        tickCount++;
        if (playerSeenThisTick || HasActiveGuardChase())
        {
            TryDispatchFromChase();
        }
        if (awareness - awarenessDecayPerTick >= 0) awareness -= awarenessDecayPerTick; else awareness = 0f;
        playerSeenThisTick = false;
        TickDebt--;
        return;
    }

    private void HandleCameraSuspicion(CameraDetector detector)
    {
        if (detector == null)
        {
            return;
        }

        awareness += cameraSuspicionBoost;
        Mathf.Clamp(awareness, 0f, maxAwareness);
    }

    private void HandleCameraDetection(CameraDetector detector)
    {
        if (detector == null)
        {
            return;
        }

        awareness += cameraDetectionBoost;
        Mathf.Clamp(awareness, 0f, maxAwareness);
    }

    public void ReportPlayerSeen(Vector2 position)
    {
        lastKnownPlayerPosition = position;
        hasLastKnownPlayerPosition = true;
        playerSeenThisTick = true;
        TryDispatchFromChase();
    }

    private bool HasActiveGuardChase()
    {
        foreach (GuardStateManager guard in guards)
        {
            if (guard != null && guard.IsChasing)
            {
                return true;
            }
        }

        return false;
    }

    private void TryDispatchFromChase()
    {
        if (!hasLastKnownPlayerPosition)
        {
            return;
        }

        if (lastDispatchTick == tickCount)
        {
            return;
        }

        if (lastDispatchTick >= 0 && dispatchCooldownTicks > 0 && tickCount - lastDispatchTick < dispatchCooldownTicks)
        {
            return;
        }

        GuardStateManager guard = FindDispatchGuard(lastKnownPlayerPosition);
        if (guard == null)
        {
            return;
        }

        guard.InvestigatePosition(lastKnownPlayerPosition);
        lastDispatchTick = tickCount;
    }

    private GuardStateManager FindDispatchGuard(Vector2 position)
    {
        GuardStateManager selected = null;
        float bestDistance = float.MaxValue;

        foreach (GuardStateManager guard in guards)
        {
            if (guard == null || guard.IsChasing)
            {
                continue;
            }

            float distance = Vector2.Distance(guard.transform.position, position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                selected = guard;
            }
        }

        if (selected != null)
        {
            return selected;
        }

        foreach (GuardStateManager guard in guards)
        {
            if (guard == null)
            {
                continue;
            }

            float distance = Vector2.Distance(guard.transform.position, position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                selected = guard;
            }
        }

        return selected;
    }

    private void UpdateAwarenessLevel()
    {
        if (awareness >= lockdownThreshold)
        {
            if (currentLevel != AwarenessLevel.Lockdown)
            {
                NotificationManager.Instance.SendNotification("Lockdown initiated!", Color.red);
                currentLevel = AwarenessLevel.Lockdown;
            }
        }
        else if (awareness >= alertThreshold)
        {
            if (currentLevel != AwarenessLevel.Alert)
            {
                NotificationManager.Instance.SendNotification("Alert level reached!", Color.orange);
                currentLevel = AwarenessLevel.Alert;
            }
        }
        else if (awareness >= suspiciousThreshold)
        {
            if (currentLevel != AwarenessLevel.Suspicious)
            {
                NotificationManager.Instance.SendNotification("Guards are catching on!", Color.yellow);
                currentLevel = AwarenessLevel.Suspicious;
            }
        }
    }

    public void MakeSound(Vector2 position, float intensity)
    {
        GuardStateManager nearest = FindDispatchGuard(position);

        if (nearest != null && Vector2.Distance(nearest.transform.position, position) <= intensity)
        {
            NotificationManager.Instance.SendNotification("Footsteps Approach...", Color.gray);
            nearest.InvestigatePosition(position);
        }
    }
    public void ReportGuardSuspicion(float suspicion)
    {
        awareness += suspicion * guardSuspicionWeight;
        Mathf.Clamp(awareness, 0f, maxAwareness);
        UpdateAwarenessLevel();
    }

    public void AnomalyIncrement(List<ItemUI> items)
    {
        float increment = items.Sum(item => item.myItem.suspicionModifier);
        awareness += increment;
        Mathf.Clamp(awareness, 0f, maxAwareness);
        UpdateAwarenessLevel();
    }
}
