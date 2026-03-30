using Godot;

namespace Utilities;

public static class Node2DExtensions
{
    public static void FlipTo(this Node2D node2D, Vector2 direction)
    {
        if (direction.X == 0f)
            return;
        
        float x      = Mathf.Sign(direction.X);
        node2D.Scale = new Vector2(x, node2D.Scale.Y);
    }

    public static void SmoothlyLookAt(this Node2D node2D, Vector2 target, float acceleration, double deltaTime)
    {
        float rad = (target - node2D.GlobalPosition).Angle();
        float dt  = (float)deltaTime;

        node2D.Rotation = Mathf.LerpAngle(node2D.Rotation, rad, acceleration * dt);
    }

    public static Vector2 GetMouseDirection(this Node2D node2D)
    {
        Vector2 mousePos = node2D.GetGlobalMousePosition();
        return node2D.GlobalPosition.DirectionTo(mousePos);
    }
}

