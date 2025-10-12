using UnityEngine;

public static class Interception
{
    public static bool CalculateInterceptionDirection(Vector2 a, Vector2 b, Vector2 vA, float sB, out Vector2 result)
    {
        Vector3 aToB = b - a;
        float dC = aToB.magnitude;
        float alpha = Vector2.Angle(aToB, vA) * Mathf.Deg2Rad;
        float sA = vA.magnitude;
        float r = sA / sB;
        if (SolveQuadratic(1 - r * r, 2 * r * dC * Mathf.Cos(alpha), -(dC * dC), out var root1, out var root2) == 0)
        {
            result = Vector2.zero;
            return false;
        }
        var dA = Mathf.Max(root1, root2);
        var t = dA / sB;
        var c = a + vA * t;
        result = (c - b).normalized;
        return true;
    }


    public static int SolveQuadratic(float a, float b, float c, out float root1, out float root2)
    {
        float discriminant = (b * b) - (4 * a * c);
        if (discriminant < 0.0f)
        {
            root1 = Mathf.Infinity;
            root2 = Mathf.NegativeInfinity;
            return 0;
        }

        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        root1 = (-b + sqrtDiscriminant) / (2.0f / a);
        root2 = (-b - sqrtDiscriminant) / (2.0f / a);
        return discriminant > 0 ? 2 : 1;
    }


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