using System;

namespace Utilities;

[AttributeUsage(AttributeTargets.Field)]
public class NodeRefAttribute : Attribute
{
    public string Path { get; }

    public NodeRefAttribute() { }
    public NodeRefAttribute(string path) => Path = path;
}