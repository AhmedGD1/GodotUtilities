using Microsoft.CodeAnalysis;

namespace GodotUtilities.SourceGenerators;

internal static class SingletonDiagnostics
{
    private const string Category = "GodotUtilities.Singleton";

    public static readonly DiagnosticDescriptor NotPartial = new(
        id: "GUSNG001",
        title: "[Singleton] class must be partial",
        messageFormat: "Class '{0}' has [Singleton] but it (or a type around it) is not declared partial, so nothing was generated",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedType = new(
        id: "GUSNG002",
        title: "[Singleton] class can't be abstract, static or generic",
        messageFormat: "Class '{0}' is abstract, static or generic (or nested in a generic type); a singleton needs one concrete, non-generic class",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RequiresNode = new(
        id: "GUSNG003",
        title: "Singleton mode requires a Node",
        messageFormat: "Class '{0}' uses SingletonMode.{1}, which needs a class that derives from Godot.Node. Derive from Node, or use SingletonMode.Lazy.",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoParameterlessConstructor = new(
        id: "GUSNG004",
        title: "Lazy singleton needs a parameterless constructor",
        messageFormat: "Class '{0}' is a SingletonMode.Lazy singleton but has no parameterless constructor to create the instance with",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MemberNameCollision = new(
        id: "GUSNG005",
        title: "Singleton member name is already in use",
        messageFormat: "Class '{0}' already declares a member named '{1}', which the [Singleton] generator needs. Rename it, or remove [Singleton].",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OptionIgnored = new(
        id: "GUSNG006",
        title: "[Singleton] option has no effect in this mode",
        messageFormat: "Class '{0}': the '{1}' option of [Singleton] is ignored here ({2})",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
