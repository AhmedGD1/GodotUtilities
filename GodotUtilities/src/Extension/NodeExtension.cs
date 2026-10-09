using Godot;

namespace GodotUtilities;

public static class NodeExtension
{
    public static void AddToGroup(this Node node) => node.AddToGroup(node.GetType().Name);

    public static bool HasNode<T>(this Node node) => node.HasNode(typeof(T).Name);

    public static T GetNode<T>(this Node node) where T : Node => node.GetNode<T>(typeof(T).Name);

    public static T GetNodeOrNull<T>(this Node node) where T : Node => node.GetNodeOrNull<T>(typeof(T).Name);

    public static T GetAutoload<T>(this Node node) where T : Node => node.GetNode<T>($"/root/{typeof(T).Name}");

    public static T GetSibling<T>(this Node node, int index) where T : Node => (T)node.GetParent().GetChild(index);
    
    public static IEnumerable<T> GetChildrenOfType<T>(this Node node) where T : Node => node.GetChildren().OfType<T>();

    public static T GetLastChild<T>(this Node node) where T : Node => node.GetChild<T>(node.GetChildCount() - 1);

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

    public static List<Node> GetAllDescendants(this Node node)
    {
        var result = new List<Node>();
        foreach (var child in node.GetChildren())
        {
            result.AddRange(child.GetAllDescendants());
            result.Add(child);
        }
        return result;
    }
}
