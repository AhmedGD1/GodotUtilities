using Godot;

namespace Utilities;

public static class CharacterBody2DExtensions
{
    public static void ApplyGravity(this CharacterBody2D controller, float gravity, double delta, float maxFallSpeed = 1000f)
    {
        if (controller.IsOnFloor())
            return;
        
        float dt = (float)delta;
        Vector2 floorDirection = -controller.UpDirection;

        controller.Velocity += floorDirection * gravity * dt;

        float speed = controller.Velocity.Dot(floorDirection);

        if (speed > maxFallSpeed)
            controller.Velocity -= floorDirection * (speed - maxFallSpeed);
    }

    public static bool IsMoving(this CharacterBody2D controller, float threshold = 0.05f)
    {
        return controller.Velocity.Length() > threshold;
    }

    public static bool IsFalling(this CharacterBody2D controller, float threshold = 0.05f)
    {
        Vector2 floorDirection = -controller.UpDirection;
        float speed            = controller.Velocity.Dot(floorDirection);

        return speed > threshold;
    }

    public static void AddForce(this CharacterBody2D controller, Vector2 force, float dt, float mass)
    {
        controller.Velocity += force * dt / mass;
    }
    
    public static void AddForce(this CharacterBody2D controller, Vector2 force, float dt)
    {
        controller.Velocity += force * dt;
    }

    public static void AddImpulse(this CharacterBody2D controller, Vector2 force)
    {
        controller.Velocity += force;
    }

    public static void AddImpulse(this CharacterBody2D controller, Vector2 force, float mass)
    {
        controller.Velocity += force / mass;
    }

    public static void Jump(this CharacterBody2D controller, float heightInPixels, float gravity)
    {
        float dir = controller.UpDirection.Y;
        controller.Velocity = new Vector2(controller.Velocity.X, dir * Mathf.Sqrt(2f * Mathf.Abs(gravity) * heightInPixels));
    }

    public static void Jump(this CharacterBody2D controller, float heightInPixels)
    {
        controller.Jump(heightInPixels, 980f);
    }

    public static void ApplyKnockbackFrom(this CharacterBody2D controller, Vector2 sourcePosition, float force)
    {
        Vector2 dir = sourcePosition.DirectionTo(controller.GlobalPosition);
        controller.AddImpulse(dir * force);
    }

    public static void ApplyKnockback(this CharacterBody2D controller, Vector2 direction, float force)
    {
        Vector2 dir = direction.Normalized();
        controller.AddImpulse(dir * force);
    }
}

