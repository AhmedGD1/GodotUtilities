using Godot;

namespace GodotUtilities;

public static class SceneTreeExtension
{
    public static T GetFirstNodeInGroup<T>(this SceneTree tree, StringName group) where T : Node
    {
        return tree.GetFirstNodeInGroup(group) as T;
    }

    public static IEnumerable<T> GetNodesInGroup<T>(this SceneTree tree, StringName group) where T : Node
    {
        return tree.GetNodesInGroup(group).OfType<T>();
    }

    public static async Task Wait(this SceneTree tree, double duration, bool ignoreTimeScale = false, bool processAlways = true)
    {
        await tree.ToSignal(
            tree.CreateTimer(duration, processAlways, ignoreTimeScale: ignoreTimeScale),
            SceneTreeTimer.SignalName.Timeout
        );
    }

    public static async Task NextIdle(this SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
