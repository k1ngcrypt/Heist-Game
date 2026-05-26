using System.Collections.Generic;
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
    [SerializeField, Min(0f)] private float maxAwareness = 100f;
    [SerializeField, Min(0f)] private float awarenessDecayPerTick = 2f;
    [SerializeField, Min(0f)] private float guardSuspicionWeight = 8f;
    [SerializeField, Range(0f, 2f)] private float corroborationMultiplier = 0.25f;

    [Header("Levels")]
    [SerializeField, Range(0f, 100f)] private float suspiciousThreshold = 25f;
    [SerializeField, Range(0f, 100f)] private float alertThreshold = 60f;
    [SerializeField, Range(0f, 100f)] private float lockdownThreshold = 90f;

    [Header("Dispatch")]
    [SerializeField, Min(0f)] private float cameraSuspicionBoost = 10f;
    [SerializeField, Min(0f)] private float cameraDetectionBoost = 25f;
    [SerializeField, Min(0)] private int dispatchCooldownTicks = 2;
    [SerializeField] private TurnManager turnManager;

    private readonly List<GuardStateManager> guards = new();
    private readonly Dictionary<CameraDetector, UnityAction> cameraSuspicionHandlers = new();
    private readonly Dictionary<CameraDetector, UnityAction> cameraDetectionHandlers = new();
    private readonly Dictionary<CameraDetector, int> cameraLastDispatchTick = new();

    private float awareness;
    private float pendingAwarenessBoost;
    private AwarenessLevel currentLevel;
    private int tickCount;

    public static AwarenessManager Instance { get; private set; }
    public float Awareness => awareness;
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

        UnityAction suspicionHandler = () => HandleCameraSuspicion(detector);
        UnityAction detectionHandler = () => HandleCameraDetection(detector);

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

        cameraLastDispatchTick.Remove(detector);
    }

    private void UnregisterAllCameras()
    {
        foreach (CameraDetector detector in new List<CameraDetector>(cameraSuspicionHandlers.Keys))
        {
            UnregisterCamera(detector);
        }
    }

    public async Awaitable OnTick()
    {
        if (!enabled)
        {
            return;
        }

        tickCount++;
        float guardContribution = CalculateGuardContribution();
        awareness = Mathf.Clamp(
            awareness + guardContribution + pendingAwarenessBoost - awarenessDecayPerTick,
            0f,
            maxAwareness);
        pendingAwarenessBoost = 0f;
        UpdateAwarenessLevel();
        TickDebt--;
        return;
    }

    private float CalculateGuardContribution()
    {
        float sumSuspicion = 0f;
        int mildSuspicionCount = 0;

        foreach (GuardStateManager guard in guards)
        {
            if (guard == null)
            {
                continue;
            }

            float ratio = guard.SuspicionRatio;
            if (ratio <= 0f)
            {
                continue;
            }

            sumSuspicion += ratio;
            if (ratio < 1f)
            {
                mildSuspicionCount++;
            }
        }

        if (sumSuspicion <= 0f)
        {
            return 0f;
        }

        float compound = 1f + Mathf.Max(0, mildSuspicionCount - 1) * corroborationMultiplier;
        return sumSuspicion * compound * guardSuspicionWeight;
    }

    private void HandleCameraSuspicion(CameraDetector detector)
    {
        if (detector == null)
        {
            return;
        }

        pendingAwarenessBoost += cameraSuspicionBoost;
        DispatchGuard(detector);
    }

    private void HandleCameraDetection(CameraDetector detector)
    {
        if (detector == null)
        {
            return;
        }

        pendingAwarenessBoost += cameraDetectionBoost;
        DispatchGuard(detector);
    }

    private void DispatchGuard(CameraDetector detector)
    {
        if (detector == null)
        {
            return;
        }

        if (dispatchCooldownTicks > 0 && cameraLastDispatchTick.TryGetValue(detector, out int lastTick))
        {
            if (tickCount - lastTick < dispatchCooldownTicks)
            {
                return;
            }
        }

        GuardStateManager guard = FindDispatchGuard(detector.transform.position);
        if (guard == null)
        {
            return;
        }

        Vector2 investigatePosition = detector.HasLastSeenPosition
            ? detector.LastSeenPosition
            : (Vector2)detector.transform.position;
        guard.InvestigatePosition(investigatePosition);
        cameraLastDispatchTick[detector] = tickCount;
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
        AwarenessLevel newLevel = AwarenessLevel.Calm;

        if (awareness >= lockdownThreshold)
        {
            newLevel = AwarenessLevel.Lockdown;
        }
        else if (awareness >= alertThreshold)
        {
            newLevel = AwarenessLevel.Alert;
        }
        else if (awareness >= suspiciousThreshold)
        {
            newLevel = AwarenessLevel.Suspicious;
        }

        currentLevel = newLevel;
    }
}
