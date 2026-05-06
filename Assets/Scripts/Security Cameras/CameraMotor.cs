using UnityEngine;

[RequireComponent(typeof(ITurnActor))]
public class CameraMotor : MonoBehaviour
{
    [SerializeField, Min(0)] private int panDegPerTick = 45;
    [SerializeField] private float minimumAngle = -45f;
    [SerializeField] private float maximumAngle = 45f;
    [SerializeField, Min(0)] private int waitTicksAtExtents = 1;
    private bool isPaused = false;
    private int direction = 1;
    private float currentAngle;
    private float minLocalAngle;
    private float maxLocalAngle;
    private ITurnActor turnActor;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        turnActor = GetComponent<ITurnActor>();
        float startAngle = transform.localEulerAngles.z;
        if (startAngle > 180f)
        {
            startAngle -= 360f;
        }

        minLocalAngle = startAngle + minimumAngle;
        maxLocalAngle = startAngle + maximumAngle;
        currentAngle = startAngle;
    }

    public void Tick()
    {
        if (!enabled)
        {
            return;
        }

        if (isPaused)
        {
            return;
        }

        currentAngle += direction * panDegPerTick;

        if (currentAngle >= maxLocalAngle)
        {
            currentAngle = maxLocalAngle;
            direction = -1;
            turnActor.TickDebt -= waitTicksAtExtents;
        }
        else if (currentAngle <= minLocalAngle)
        {
            currentAngle = minLocalAngle;
            direction = 1;
            turnActor.TickDebt -= waitTicksAtExtents;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    public void PauseRotation()
    {
        isPaused = true;
    }

    public void ResumeRotation()
    {
        isPaused = false;
    }
}
