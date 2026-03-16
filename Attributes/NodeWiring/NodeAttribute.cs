using System;

namespace Utilities;

[AttributeUsage(AttributeTargets.Field)]
public class NodeAttribute : Attribute
{
    public string Path { get; }

    public NodeAttribute() { }
    public NodeAttribute(string path) => Path = path;
}