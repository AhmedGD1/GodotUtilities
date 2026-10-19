namespace GodotUtilities;

/// <summary>
/// Marks a field or property to be automatically wired to a node in the scene tree.
/// If no path is given, the node is found from the member name.
/// </summary>
/// <param name="nodePath">
/// Optional explicit node path, relative to this node (e.g. "UI/HealthBar").
/// </param>
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
