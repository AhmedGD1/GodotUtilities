using Godot;

namespace GodotUtilities;

public static class ResourceExtension
{
    public static T Duplicate<T>(this Resource resource, bool deep = false) where T : Resource
    {
        return (T)resource.Duplicate(deep);
    }

    public static T DuplicateDeep<T>(this Resource resource, Resource.DeepDuplicateMode deepSubresourcesMode = Resource.DeepDuplicateMode.Internal) where T : Resource
    {
        return (T)resource.DuplicateDeep(deepSubresourcesMode);
    }
}

