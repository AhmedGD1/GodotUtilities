using Godot;

namespace Utilities;

public static class NodeExtensions
{
    #region Child Search

    public static T GetChildOfType<T>(this Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
            if (child is T t)
                return t;
        return null;
    }

    public static T GetChildOfTypeRecursive<T>(this Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T t)
                return t;

            var result = child.GetChildOfTypeRecursive<T>();
            
            if (result != null)
                return result;
        }

        return null;
    }

    public static bool TryGetChildOfType<T>(this Node node, out T result) where T : Node
    {
        result = node.GetChildOfType<T>();
        return result != null;
    }

    public static bool TryGetChildOfTypeRecursive<T>(this Node node, out T result) where T : Node
    {
        result = node.GetChildOfTypeRecursive<T>();
        return result != null;
    }

    #endregion

    #region Parent Search

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

    #endregion

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
