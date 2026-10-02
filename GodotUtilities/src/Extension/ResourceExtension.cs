using Godot;

namespace GodotUtilities;

public static class ResourceExtension
{
    public static T Duplicate<T>(this Resource resource, bool deep = false) where T : Resource
    {
        return (T)resource.Duplicate(deep);
    }
}

