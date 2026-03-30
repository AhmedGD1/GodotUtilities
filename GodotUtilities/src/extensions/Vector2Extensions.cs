using Godot;

namespace Utilities;

public static class Vector2Extensions
{
    public static Vector2 RotatedDegrees(this Vector2 vector, float deg) =>
        vector.Rotated(Mathf.DegToRad(deg));
        
    public static bool IsWithinDistanceSquared(this Vector2 v1, Vector2 v2, float distance) =>
        v1.DistanceSquaredTo(v2) <= distance * distance;

    public static bool IsWithinDistance(this Vector2 v1, Vector2 v2, float distance) =>
        v1.DistanceTo(v2) <= distance;

    public static Vector2 NormalizeIfNotZero(this Vector2 vector) =>
        vector.LengthSquared() > 0.001f ? vector.Normalized() : Vector2.Zero;

    public static Vector3 ToVector3XY(this Vector2 vector) => new Vector3(vector.X, vector.Y, 0f);
    public static Vector3 ToVector3XZ(this Vector2 vector) => new Vector3(vector.X, 0f, vector.Y);
}

