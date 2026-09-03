using Godot;

namespace GodotUtilities;

public static class NodeExtension
{
    public static bool HasNode<T>(this Node node)
    {
        return node.HasNode(typeof(T).Name);
    }
    
    public static T GetNode<T>(this Node node) where T : Node
    {
        string name = typeof(T).Name;
        return node.GetNode<T>(name);
    }
    
    public static T GetAutoload<T>(this Node node) where T : Node
    {
        string path = $"/root/{typeof(T).Name}";
        return node.GetNode<T>(path);
    }

    public static IEnumerable<T> GetChildrenOfType<T>(this Node node) where T : Node
    {
        return node.GetChildren().OfType<T>();
    }

    public static void QueueFreeChildren(this Node node)
    {
        foreach (var child in node.GetChildren())
            child.QueueFree();
    }

    public static void AddChildDeferred(this Node node, Node child)
    {
        node.CallDeferred(Node.MethodName.AddChild, child);
    }

    public static T GetChildOfType<T>(this Node node, bool recursive = false) where T : Node
    {
        return node.TryGetChildOfType<T>(out var result, recursive) ? result : null;
    }

    public static bool TryGetChildOfType<T>(this Node node, out T result, bool recursive = false) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T t)
            {
                result = t;
                return true;
            }

            if (recursive && child.TryGetChildOfType(out result, recursive: true))
                return true;
        }

        result = default;
        return false;
    }
}
