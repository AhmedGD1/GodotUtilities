using Godot;

namespace GodotUtilities;

public static class Vector3Extension
{
    public static Vector3 RotatedDegrees(this Vector3 value, Vector3 axis, float degrees)
    {
        return value.Rotated(axis, Mathf.DegToRad(degrees));
    }

    public static bool IsWithinDistanceSquared(this Vector3 a, Vector3 b, float distance)
    {
        return a.DistanceSquaredTo(b) <= distance * distance;
    }

    public static Vector2 ToVector2(this Vector3 value) => new(value.X, value.Y);
}
