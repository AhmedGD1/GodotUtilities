using Microsoft.CodeAnalysis;

namespace GodotUtilities.SourceGenerators.NodeWiring;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor ContainingTypeNotPartial = new(
        id: "GUNOD001",
        title: "Containing type must be partial",
        messageFormat: "'{0}' has members marked with [Node], but enclosing type '{1}' is not declared 'partial'; WireNodes() cannot be generated",
        category: "GodotUtilities.NodeWiring",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContainingTypeNotNode = new(
        id: "GUNOD002",
        title: "Containing type must derive from Godot.Node",
        messageFormat: "'{0}' has members marked with [Node] but does not derive from Godot.Node; GetNode<T>() is unavailable",
        category: "GodotUtilities.NodeWiring",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MemberTypeNotNode = new(
        id: "GUNOD003",
        title: "[Node] member type must derive from Godot.Node",
        messageFormat: "'{0}.{1}' is marked with [Node] but its type '{2}' does not derive from Godot.Node",
        category: "GodotUtilities.NodeWiring",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MemberIsStatic = new(
        id: "GUNOD004",
        title: "[Node] member must be an instance member",
        messageFormat: "'{0}.{1}' is marked with [Node] but is static; static members cannot be wired",
        category: "GodotUtilities.NodeWiring",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyHasNoSetter = new(
        id: "GUNOD005",
        title: "[Node] property must have an accessible 'set' accessor",
        messageFormat: "'{0}.{1}' is marked with [Node] but has no accessible 'set' accessor",
        category: "GodotUtilities.NodeWiring",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyIsInitOnly = new(
        id: "GUNOD008",
        title: "[Node] property cannot use an 'init' accessor",
        messageFormat: "'{0}.{1}' is marked with [Node] but its 'set' accessor is 'init'-only; WireNodes() assigns after construction and cannot use an init accessor",
        category: "GodotUtilities.NodeWiring",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateWireTarget = new(
        id: "GUNOD006",
        title: "Duplicate [Node] resolution target",
        messageFormat: "'{0}.{1}' resolves to the same node path \"{2}\" as another [Node] member in the same type",
        category: "GodotUtilities.NodeWiring",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor EmptyExplicitPath = new(
        id: "GUNOD007",
        title: "[Node] explicit path is empty",
        messageFormat: "'{0}.{1}' has an empty explicit [Node] path; remove the argument to use the default name conversion instead",
        category: "GodotUtilities.NodeWiring",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor FieldIsReadOnly = new(
        id: "GUNOD009",
        title: "[Node] field cannot be readonly",
        messageFormat: "'{0}.{1}' is marked with [Node] but is readonly; WireNodes() assigns the field after construction, so it must be writable",
        category: "GodotUtilities.NodeWiring",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyIsRequired = new(
        id: "GUNOD010",
        title: "[Node] property cannot be required",
        messageFormat: "'{0}.{1}' is marked with [Node] but is required; required properties must be set during object initialisation, but WireNodes() is called later",
        category: "GodotUtilities.NodeWiring",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
