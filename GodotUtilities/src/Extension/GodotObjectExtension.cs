using Godot;

namespace GodotUtilities;

public static class GodotObjectExtension
{
    public static bool IsNullOrInvalid(this GodotObject obj)
    {
        return obj is null || !GodotObject.IsInstanceValid(obj);
    }
}
