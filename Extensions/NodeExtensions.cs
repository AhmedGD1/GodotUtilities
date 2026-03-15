using Godot;

namespace Utilities;

public static class NodeExtensions
{
    public static T GetChildOfType<T>(this Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
            if (child is T t)
                return t;
        return null;
    }

    public static T GetParentOfType<T>(this Node node) where T : Node
    {
        Node current = node.GetParent();

        while (current != null)
        {
            if (current is T t)
                return t;
            
            current = current.GetParent();
        }

        return null;
    }

    public static void DeleteChildren(this Node node)
    {
        foreach (var child in node.GetChildren())
            child.QueueFree();
    }

    public static void AddChildDeferred(this Node node, Node child)
    {
        node.CallDeferred("add_child", child);
    }

}
