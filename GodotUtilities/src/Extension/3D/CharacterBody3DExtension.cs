using Godot;

namespace GodotUtilities;

public static class CharacterBody3DExtension
{
    public static float ApplyGravity(this CharacterBody3D body, double dt,
        float gravity = 9.8f, float maxFallSpeed = float.PositiveInfinity)
    {
        if (body.IsOnFloor())
            return 0f;

        Vector3 down = -body.UpDirection;
        body.Velocity += down * gravity * (float)dt;

        float fallSpeed = body.Velocity.Dot(down);
        
        if (fallSpeed > maxFallSpeed)
        {
            body.Velocity -= down * (fallSpeed - maxFallSpeed);
            fallSpeed = maxFallSpeed;
        }
        
        return fallSpeed;
    }

    public static float GetHorizontalSpeed(this CharacterBody3D body)
    {
        return body.Velocity.Slide(body.UpDirection).Length();
    }
    
    public static float GetVerticalSpeed(this CharacterBody3D body)
    {
        return body.Velocity.Dot(body.UpDirection);
    }
}
