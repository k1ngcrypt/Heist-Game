using UnityEngine;
using UnityEngine.Events;

public class CameraHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 1;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite disabledSprite;

    [Header("Linked Components")]
    [SerializeField] private CameraMotor motor;
    [SerializeField] private CameraDetector detector;
    [SerializeField] private CameraVisuals visuals;

    [Header("Events")]
    [SerializeField] private UnityEvent onCameraDisabled;

    private int currentHealth;
    private bool isDisabled = false;

    public bool IsDisabled => isDisabled;
    public UnityEvent OnCameraDisabled => onCameraDisabled;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

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
    }

    public void TakeDamage(int amount)
    {
        if (isDisabled || amount <= 0)
        {
            return;
        }

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            DisableCamera();
        }
    }

    public void HackAndDisable()
    {
        if (isDisabled)
        {
            return;
        }

        DisableCamera();
    }

    private void DisableCamera()
    {
        isDisabled = true;
        currentHealth = 0;

        if (motor != null)
        {
            motor.enabled = false;
        }

        if (detector != null)
        {
            detector.enabled = false;
        }

        if (visuals != null)
        {
            visuals.enabled = false;
        }

        if (spriteRenderer != null && disabledSprite != null)
        {
            spriteRenderer.sprite = disabledSprite;
        }

        onCameraDisabled?.Invoke();
    }
}
