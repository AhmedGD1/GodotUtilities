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

    public static T Instantiate<T>(this PackedScene packedScene, Node parent, Vector2? globalPos = null) where T : Node2D
    {
        var instance = packedScene.Instantiate<T>();
        parent.AddChild(instance);

        if (globalPos.HasValue)
            instance.GlobalPosition = globalPos.Value;
        return instance;
    }
}
