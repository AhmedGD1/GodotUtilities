using Godot;

namespace GodotUtilities;

public static class SceneTreeExtension
{
    public static T GetFirstNodeInGroup<T>(this SceneTree tree, StringName group) where T : Node => (T)tree.GetFirstNodeInGroup(group);

    public static T GetFirstNodeInGroup<T>(this SceneTree tree) where T : Node => (T)tree.GetFirstNodeInGroup(typeof(T).Name);

    public static IEnumerable<T> GetNodesInGroup<T>(this SceneTree tree, StringName group) where T : Node => tree.GetNodesInGroup(group).OfType<T>();

    public static IEnumerable<T> GetNodesInGroup<T>(this SceneTree tree) where T : Node => tree.GetNodesInGroup(typeof(T).Name).OfType<T>();
    
    public static async Task Wait(this SceneTree tree, double duration, bool ignoreTimeScale = false, bool processAlways = true)
    {
        var timer = tree.CreateTimer(duration, processAlways, ignoreTimeScale: ignoreTimeScale);
        await tree.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
    }

    public static async Task NextIdle(this SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
