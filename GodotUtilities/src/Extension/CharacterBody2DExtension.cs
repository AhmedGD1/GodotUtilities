using Godot;

namespace GodotUtilities;

public static class CharacterBody2DExtension
{
    public static void ApplyGravity(this CharacterBody2D body, double dt,
            float gravity = 980f, float maxFallSpeed = float.PositiveInfinity)
    {
        if (body.IsOnFloor())
            return;

        Vector2 down = -body.UpDirection;
        body.Velocity += down * gravity * (float)dt;

        float dot = body.Velocity.Dot(down);

        if (dot > maxFallSpeed)
            body.Velocity -= down * (dot - maxFallSpeed);
    }

    public static float GetHorizontalSpeed(this CharacterBody2D body)
    {
        return body.Velocity.Slide(body.UpDirection).Length();
    }
    
    public static float GetVerticalSpeed(this CharacterBody2D body)
    {
        return Mathf.Abs(body.Velocity.Dot(body.UpDirection));
    }
}
