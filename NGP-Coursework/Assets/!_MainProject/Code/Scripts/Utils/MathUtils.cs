using UnityEngine;

// A variety of utility functions relating to maths.

public static class MathUtils
{
    /// <summary>
    ///     Returns 0 if <paramref name="value"/> exceeds <paramref name="maxValueExclusive"/>,<br/>
    ///     <paramref name="maxValueExclusive"/> if <paramref name="value"/> is below 0,<br/>
    ///     or <paramref name="value"/> if it is within their range.
    /// </summary>
    public static int Loop(int value, int maxValueExclusive) => Loop(value, 0, maxValueExclusive);
    /// <summary>
    ///     Returns <paramref name="minValueInclusive"/> if <paramref name="value"/> exceeds <paramref name="maxValueExclusive"/>,<br/>
    ///     <paramref name="maxValueExclusive"/> if <paramref name="value"/> is below <paramref name="minValueInclusive"/>,<br/>
    ///     or <paramref name="value"/> if it is within their range.
    /// </summary>
    public static int Loop(int value, int minValueInclusive, int maxValueExclusive)
    {
        if (value >= maxValueExclusive)
            return minValueInclusive;
        else if (value < minValueInclusive)
            return maxValueExclusive - 1;
        else
            return value;
    }
}


public static class Interception
{
    /// <summary>
    ///     Calculate the required direction to intercept a moving target
    /// </summary>
    /// <returns> True if we can intercept the target, false if we cannot.</returns>
    // From: 'https://discussions.unity.com/t/formula-to-calculate-a-position-to-fire-at/48516/5'.
    public static bool CalculateInterceptionDirection(Vector3 targetPos, Vector3 targetVelocity, Vector3 currentPos, float speed, out Vector3 interceptionDirection)
    {
        Vector3 targetDir = targetPos - currentPos;

        float sqrSpeed = speed * speed;
        float sqrTargetSpeed = targetVelocity.sqrMagnitude;
        float fDot1 = Vector3.Dot(targetDir, targetVelocity);
        float sqrTargetDistance = targetDir.sqrMagnitude;
        float d = (fDot1 * fDot1) - sqrTargetDistance * (sqrTargetSpeed - sqrSpeed);
        if (d < 0.1f)  // negative == no possible course because the interceptor isn't fast enough
        {
            interceptionDirection = Vector3.zero;
            return false;
        }
        float sqrt = Mathf.Sqrt(d);
        float S1 = (-fDot1 - sqrt) / sqrTargetDistance;
        float S2 = (-fDot1 + sqrt) / sqrTargetDistance;

        if (S1 < 0.0001f)
        {
            if (S2 < 0.0001f)
            {
                interceptionDirection = Vector3.zero;
                return false;
            }
            else
            {
                interceptionDirection = (S2) * targetDir + targetVelocity;
                return true;
            }
        }
        else if (S2 < 0.0001f)
        {
            interceptionDirection = (S1) * targetDir + targetVelocity;
            return true;
        }
        else if (S1 < S2)
        {
            interceptionDirection = (S2) * targetDir + targetVelocity;
            return true;
        }
        else
        {
            interceptionDirection = (S1) * targetDir + targetVelocity;
            return true;
        }
    }
}