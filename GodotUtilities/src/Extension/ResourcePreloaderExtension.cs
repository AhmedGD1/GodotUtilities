using Godot;

namespace GodotUtilities;

public static class ResourcePreloaderExtension
{
    public static T InstantiateSceneOrNull<T>(this ResourcePreloader resourcePreloader, StringName name) where T : Node
    {
        if (!resourcePreloader.HasResource(name))
        {
            GD.PushError($"[Resource Preloader] resource with name '{name}' does not exist.");
            return null;
        }

        if (resourcePreloader.GetResource(name) is not PackedScene packedScene)
        {
            GD.PushError($"resource with name '{name}' is not a {nameof(PackedScene)}");
            return null;
        }

        return packedScene.InstantiateOrNull<T>();
    }
    
    public static T InstantiateSceneOrNull<T>(this ResourcePreloader resourcePreloader) where T : Node
    {
        string name = typeof(T).Name;

        if (!resourcePreloader.HasResource(name))
            name = typeof(T).Name.ToSnakeCase();

        if (!resourcePreloader.HasResource(name))
        {
            GD.PushError($"[Resource Preloader] resource with type '{typeof(T).Name}' does not exist.");
            return null;
        }

        if (resourcePreloader.GetResource(name) is not PackedScene packedScene)
        {
            GD.PushError($"resource with name '{name}' is not a {nameof(PackedScene)}");
            return null;
        }

        return packedScene.InstantiateOrNull<T>();    
    }
}
