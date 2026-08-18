using Godot;

namespace GodotUtilities;

public static class Camera3DExtension
{
    public static Vector3 GetMouseRayDirection(this Camera3D camera)
    {
        Vector2 mouse = camera.GetViewport().GetMousePosition();
        return camera.ProjectRayNormal(mouse);
    }

    public static Vector3 GetMouseRayOrigin(this Camera3D camera)
    {
        Vector2 mouse = camera.GetViewport().GetMousePosition();
        return camera.ProjectRayOrigin(mouse);
    }
}
