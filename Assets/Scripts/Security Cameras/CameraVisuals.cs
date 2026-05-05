using UnityEngine;
using UnityEngine.Rendering.Universal;
public class CameraVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraDetector detector;
    [SerializeField] private Light2D cameraLight;

    [Header("Colors")]
    [SerializeField] private Color scanningColor = new(0.2f, 1f, 0.3f, 0.2f);
    [SerializeField] private Color suspiciousColor = new(1f, 0.8f, 0.2f, 0.35f);
    [SerializeField] private Color detectedColor = new(1f, 0.2f, 0.2f, 0.45f);

    private void Awake()
    {
        if (detector == null)
        {
            detector = GetComponent<CameraDetector>();
        }

        if (cameraLight == null)
        {
            cameraLight = GetComponentInChildren<Light2D>(true);
        }

        UpdateLightSettings();
    }

    private void OnValidate()
    {
        if (detector == null)
        {
            detector = GetComponent<CameraDetector>();
        }

        if (cameraLight == null)
        {
            cameraLight = GetComponentInChildren<Light2D>(true);
        }

        UpdateLightSettings();
        UpdateColor();
    }

    public void Tick()
    {
        if (!enabled)
        {
            return;
        }

        UpdateLightSettings();
        UpdateColor();
    }

    private void UpdateLightSettings()
    {
        cameraLight.pointLightOuterRadius = detector.DetectionRange;
        cameraLight.pointLightOuterAngle = detector.ViewAngle;
        cameraLight.pointLightInnerAngle = detector.ViewAngle * 0.8f;
    }

    private void UpdateColor()
    {
        float normalizedSuspicion = detector.Suspicion * 0.01f;
        Color target;

        if (normalizedSuspicion < 0.5f)
        {
            target = Color.Lerp(scanningColor, suspiciousColor, normalizedSuspicion * 2f);
        }
        else
        {
            target = Color.Lerp(suspiciousColor, detectedColor, (normalizedSuspicion - 0.5f) * 2f);
        }

        cameraLight.color = target;
    }
}
