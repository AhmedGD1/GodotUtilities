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
        return resourcePreloader.InstantiateSceneOrNull<T>(typeof(T).Name);
    }
}
