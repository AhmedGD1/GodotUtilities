using Godot;

namespace GodotUtilities;

public static class Vector2Extension
{
    public static bool IsWithinDistanceSquared(this Vector2 v1, Vector2 v2, float distance)
    {
        return v1.DistanceSquaredTo(v2) <= (distance * distance);
    }

    public static float AngleDegrees(this Vector2 value) => Mathf.RadToDeg(value.Angle());

    public static Vector2 RotatedDegrees(this Vector2 value, float deg) => value.Rotated(Mathf.DegToRad(deg));

    public static Vector3 ToVector3(this Vector2 value) => new(value.X, value.Y, 0f);

    public static Vector3 ToVector3XZ(this Vector2 value) => new(value.X, 0f, value.Y);
    
    public static Vector3 ToVector3YZ(this Vector2 value) => new(0f, value.X, value.Y);

    public static Vector2I ToVector2I(this Vector2 value) => new(Mathf.RoundToInt(value.X), Mathf.RoundToInt(value.Y)); 
}
