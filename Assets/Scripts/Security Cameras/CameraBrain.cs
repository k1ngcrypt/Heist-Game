using UnityEngine;

[RequireComponent(typeof(CameraMotor), typeof(CameraDetector), typeof(CameraVisuals))]
[RequireComponent(typeof(CameraHealth))]
public class CameraBrain : MonoBehaviour
{
    [SerializeField] private CameraMotor motor;
    [SerializeField] private CameraDetector detector;
    [SerializeField] private CameraVisuals visuals;
    [SerializeField] private CameraHealth health;

    private void Awake()
    {
        if (motor == null)
        {
            motor = GetComponent<CameraMotor>();
        }

        if (detector == null)
        {
            detector = GetComponent<CameraDetector>();
        }

        if (visuals == null)
        {
            visuals = GetComponent<CameraVisuals>();
        }

        if (health == null)
        {
            health = GetComponent<CameraHealth>();
        }
    }

    private void OnEnable()
    {
        if (detector != null)
        {
            detector.OnSuspicionStarted.AddListener(HandleSuspicionStarted);
            detector.OnSuspicionCleared.AddListener(HandleSuspicionCleared);
            detector.OnPlayerDetected.AddListener(HandlePlayerDetected);
        }

        if (health != null)
        {
            health.OnCameraDisabled.AddListener(HandleCameraDisabled);
        }
    }

    private void OnDisable()
    {
        if (detector != null)
        {
            detector.OnSuspicionStarted.RemoveListener(HandleSuspicionStarted);
            detector.OnSuspicionCleared.RemoveListener(HandleSuspicionCleared);
            detector.OnPlayerDetected.RemoveListener(HandlePlayerDetected);
        }

        if (health != null)
        {
            health.OnCameraDisabled.RemoveListener(HandleCameraDisabled);
        }
    }

    private void HandleSuspicionStarted()
    {
        if (motor != null && motor.enabled)
        {
            motor.PauseRotation();
        }
    }

    private void HandleSuspicionCleared()
    {
        if (health != null && health.IsDisabled)
        {
            return;
        }

        if (motor != null && motor.enabled)
        {
            motor.ResumeRotation();
        }
    }

    private void HandlePlayerDetected()
    {
        if (motor != null && motor.enabled)
        {
            motor.PauseRotation();
        }
    }

    private void HandleCameraDisabled()
    {
        if (motor != null)
        {
            motor.PauseRotation();
        }
    }
}
