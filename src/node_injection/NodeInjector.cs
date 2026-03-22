using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System;
using Godot;

namespace Utilities;

public static class NodeInjector
{
    // safe under parallel scene loading
    private static readonly ConcurrentDictionary<Type, NodeMember[]> cache = new();

    public static void InjectNodes(this Node root)
    {
        var type    = root.GetType();
        var members = cache.GetOrAdd(type, BuildCache);

        foreach (var member in members)
        {
            var node = member.Path != null
                ? root.GetNodeOrNull(member.Path)
                : FindFirstChildOfType(root, member.FieldInfo.FieldType);

            if (node is null)
            {
                GD.PrintErr($"[WireNodes] Could not find node for " +
                            $"'{member.FieldInfo.FieldType.Name} {member.FieldInfo.Name}' " +
                            $"in '{type.Name}'");
                continue;
            }

            member.FieldInfo.SetValue(root, node);
        }
    }

    private static NodeMember[] BuildCache(Type type)
    {
        var result   = new List<NodeMember>();
        var bindings = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        foreach (var field in type.GetFields(bindings))
        {
            var attribute = field.GetCustomAttribute<NodeRefAttribute>();

            if (attribute is not null)
                result.Add(new NodeMember(field, attribute.Path));
        }

        return result.ToArray();
    }

    private static Node FindFirstChildOfType(Node parent, Type type)
    {
        foreach (var child in parent.GetChildren())
        {
            if (type.IsAssignableFrom(child.GetType()))
                return child;

            var result = FindFirstChildOfType(child, type);
            
            if (result != null)
                return result;
        }

        return null;
    }
}
