using Godot;

namespace GodotUtilities;

public static class Node2DExtension
{
    public static Vector2 GetMouseDirection(this Node2D node)
    {
        Vector2 mousePos = node.GetGlobalMousePosition();
        return node.GlobalPosition.DirectionTo(mousePos);
    }
}
