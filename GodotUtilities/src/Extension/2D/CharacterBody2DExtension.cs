using Godot;

namespace GodotUtilities;

public static class CharacterBody2DExtension
{
    public static float ApplyGravity(this CharacterBody2D body, double dt,
        float gravity = 980f, float maxFallSpeed = float.PositiveInfinity)
    {
        if (body.IsOnFloor())
            return 0f;

        Vector2 down = -body.UpDirection;
        body.Velocity += down * gravity * (float)dt;

        float fallSpeed = body.Velocity.Dot(down);
        
        if (fallSpeed > maxFallSpeed)
        {
            body.Velocity -= down * (fallSpeed - maxFallSpeed);
            fallSpeed = maxFallSpeed;
        }
        
        return fallSpeed;
    }

    public static float GetHorizontalSpeed(this CharacterBody2D body)
    {
        return body.Velocity.Slide(body.UpDirection).Length();
    }
    
    public static float GetVerticalSpeed(this CharacterBody2D body)
    {
        return body.Velocity.Dot(body.UpDirection);
    }
}
