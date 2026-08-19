using Godot;

namespace GodotUtilities;

public static class PackedSceneExtension
{
    public static T InstantiateOrFree<T>(this PackedScene scene) where T : class
    {
        var node = scene.Instantiate();
        if (node is T t)
            return t;

        node.QueueFree();
        GD.PushWarning($"Could not instance PackedScene {scene} as {typeof(T).Name}");
        return null;
    }

    public static T Instantiate<T>(this PackedScene scene, Node parent, bool deferredAddChild = false) where T : Node
    {
        var instance = scene.InstantiateOrFree<T>();

        if (deferredAddChild)
            parent.CallDeferred(Node.MethodName.AddChild, instance);
        else
            parent.AddChild(instance);
        
        return instance;
    }
}
