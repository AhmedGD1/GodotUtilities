using Godot;

namespace Utilities;

public static class Vector2Extensions
{
    public static Vector2 RotatedDegrees(this Vector2 vector, float deg)
    {
        return vector.Rotated(Mathf.DegToRad(deg));
    }

    public static bool IsWithinDistanceSquared(this Vector2 v1, Vector2 v2, float distance)
    {
        return v1.DistanceSquaredTo(v2) <= distance * distance;
    }

    public static bool IsWithinDistance(this Vector2 v1, Vector2 v2, float distance)
    {
        return v1.DistanceTo(v2) <= distance;
    }
}

