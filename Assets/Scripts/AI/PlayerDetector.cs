using UnityEngine;

public static class DetectionUtils
{
    public static bool IsDetected(
        Vector2 observerPos,
        Vector2 observerForward,
        Transform target,
        float range,
        float fovAngle,
        ContactFilter2D filter,
        RaycastHit2D[] hitBuffer)
    {
        Vector2 toTarget = (Vector2)(target.position - (Vector3)observerPos);
        float sqrDistance = toTarget.sqrMagnitude;

        // 1. Distance Check
        if (sqrDistance > range * range)
            return false;

        // 2. FOV Check
        // Using observerForward allows this to work for any orientation
        float angleToTarget = Vector2.Angle(observerForward, toTarget);
        if (angleToTarget > fovAngle * 0.5f)
            return false;

        // 3. Line of Sight Check
        float distance = Mathf.Sqrt(sqrDistance);
        int hitCount = Physics2D.Raycast(observerPos, toTarget.normalized, filter, hitBuffer, distance);

        return hitCount == 0;
    }
}