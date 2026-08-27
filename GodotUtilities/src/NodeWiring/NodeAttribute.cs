using System;

namespace GodotUtilities;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class NodeAttribute(string nodePath = null) : Attribute
{
    /// <summary>
    /// Optional explicit node path (relative to this node), e.g. "UI/HealthBar".
    /// When omitted, the path is derived from the member name (tried in PascalCase,
    /// snake_case, and camelCase form, then as a unique name, then by matching a direct
    /// child node - see NodeWiringGenerator).
    /// </summary>
    public string NodePath { get; } = nodePath;
}
