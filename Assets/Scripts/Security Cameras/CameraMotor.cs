using System.Collections;
using UnityEngine;

public class CameraMotor : MonoBehaviour
{
    [SerializeField, Min(0f)] private float panSpeed = 45f;
    [SerializeField] private float minimumAngle = -45f;
    [SerializeField] private float maximumAngle = 45f;
    [SerializeField, Min(0f)] private float waitTimeAtExtents = 0.25f;

    private bool isPaused;
    private int direction = 1;
    private float currentAngle;
    private float minLocalAngle;
    private float maxLocalAngle;
    private Coroutine waitCoroutine;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        float startAngle = transform.localEulerAngles.z;
        if (startAngle > 180f)
        {
            startAngle -= 360f;
        }

        minLocalAngle = startAngle + minimumAngle;
        maxLocalAngle = startAngle + maximumAngle;
        currentAngle = startAngle;
    }

    private void Update()
    {
        if (isPaused || waitCoroutine != null)
        {
            return;
        }

        currentAngle += direction * panSpeed * Time.deltaTime;

        if (currentAngle >= maxLocalAngle)
        {
            currentAngle = maxLocalAngle;
            direction = -1;
            waitCoroutine = StartCoroutine(WaitAtExtent());
        }
        else if (currentAngle <= minLocalAngle)
        {
            currentAngle = minLocalAngle;
            direction = 1;
            waitCoroutine = StartCoroutine(WaitAtExtent());
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

    private IEnumerator WaitAtExtent()
    {
        if (waitTimeAtExtents > 0f)
        {
            yield return new WaitForSeconds(waitTimeAtExtents);
        }

        waitCoroutine = null;
    }
}
