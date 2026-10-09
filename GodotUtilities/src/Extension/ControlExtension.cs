using Godot;

namespace GodotUtilities;

public static class ControlExtension
{
    public static void CenterPivotOffset(this Control control)
    {
        control.PivotOffset = control.Size / 2f;
    }

    public static Vector2 GetMouseDirection(this Control control, bool fromCenter = true)
    {
        Vector2 center = control.Size / 2f;
        Vector2 mousePos = control.GetGlobalMousePosition();
        Vector2 point = fromCenter ? control.GlobalPosition + center : control.GlobalPosition;

        return point.DirectionTo(mousePos);
    }
}
