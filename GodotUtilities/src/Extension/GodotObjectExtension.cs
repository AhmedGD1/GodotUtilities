using Godot;

namespace GodotUtilities;

public static class GodotObjectExtensions
{
    public static bool IsNullOrInvalid(this GodotObject obj)
    {
        return obj is null || !GodotObject.IsInstanceValid(obj);
    }
}
