using Godot;

namespace GodotUtilities;

public static class Vector2Extensions
{
    public static Vector2 RotatedDeg(this Vector2 value, float deg)
    {
        return value.Rotated(Mathf.DegToRad(deg));
    }

    public static bool IsWithinDistanceSquared(this Vector2 v1, Vector2 v2, float distance)
    {
        return v1.DistanceSquaredTo(v2) <= (distance * distance);
    }
}
