using System.Reflection;

namespace Utilities;

internal readonly struct NodeMember
{
    public readonly FieldInfo FieldInfo;
    public readonly string Path;

    public NodeMember(FieldInfo fieldInfo, string path)
    {
        FieldInfo = fieldInfo;
        Path      = path;
    }
}