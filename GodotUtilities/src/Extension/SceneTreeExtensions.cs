using Godot;

namespace GodotUtilities;

public static class SceneTreeExtensions
{
    public static T GetFirstNodeInGroup<T>(this SceneTree tree, StringName group) where T : Node
    {
        return tree.GetFirstNodeInGroup(group) as T;
    }

    public static IEnumerable<T> GetNodesInGroup<T>(this SceneTree tree, StringName group) where T : Node
    {
        return tree.GetNodesInGroup(group).OfType<T>();
    }

    public static SignalAwaiter Wait(this SceneTree tree, double duration)
    {
        return tree.ToSignal(tree.CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);
    }

    public static SignalAwaiter NextIdle(this SceneTree tree)
    {
        return tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
