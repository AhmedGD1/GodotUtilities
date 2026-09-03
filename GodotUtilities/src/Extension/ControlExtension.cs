using Godot;

namespace GodotUtilities;

public static class ControlExtension
{
    public static void CenterPivotOffset(this Control control)
    {
        control.PivotOffset = control.Size / 2f;
    }

    public static Vector2 GetMouseDirection(this Control control)
    {
        return control.GlobalPosition.DirectionTo(control.GetGlobalMousePosition());
    }
}
