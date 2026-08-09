using Microsoft.CodeAnalysis;

namespace GodotUtilities.SourceGenerators;

internal static class EventHandlerDiagnostics
{
    public static readonly DiagnosticDescriptor MissingEventType = new(
        id: "GUEVT001",
        title: "Event handler has no inferable event type",
        messageFormat: "Method '{0}' has no parameters and no explicit event type. Add a parameter or use [EventHandler(typeof(YourEvent))].",
        category: "GodotUtilities.Events",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ParameterTypeMismatch = new(
        id: "GUEVT002",
        title: "Event handler parameter type does not match explicit event type",
        messageFormat: "Method '{0}': explicit type '{1}' does not match parameter type '{2}'",
        category: "GodotUtilities.Events",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TooManyParameters = new(
        id: "GUEVT003",
        title: "Event handler has too many parameters",
        messageFormat: "Method '{0}' must take zero parameters or exactly one event parameter, found {1}",
        category: "GodotUtilities.Events",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateHandlerForType = new(
        id: "GUEVT004",
        title: "Duplicate event handler for the same event type",
        messageFormat: "Class '{0}' has multiple [EventHandler] methods for event type '{1}': '{2}' and '{3}'. Only one handler per event type is allowed per class.",
        category: "GodotUtilities.Events",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContainingClassNotPartial = new(
        id: "GUEVT005",
        title: "Class containing [EventHandler] methods must be partial",
        messageFormat: "Class '{0}' has [EventHandler] methods but is not declared 'partial'. The event bus source generator needs to add a WireEvents() method to it.",
        category: "GodotUtilities.Events",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContainingClassNotNode = new(
        id: "GUEVT006",
        title: "Class containing [EventHandler] methods must derive from Godot.Node",
        messageFormat: "Class '{0}' has [EventHandler] methods but does not derive from Godot.Node",
        category: "GodotUtilities.Events",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StaticMethodNotSupported = new(
        id: "GUEVT007",
        title: "Event handler methods cannot be static",
        messageFormat: "Method '{0}' is marked [EventHandler] but is static. Event handlers must be instance methods.",
        category: "GodotUtilities.Events",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedClassNotSupported = new(
        id: "GUEVT008",
        title: "[EventHandler] is not supported on nested classes",
        messageFormat: "Class '{0}' declares [EventHandler] methods but is a nested type. Move the class to namespace scope, or wire events manually via EventBus.AddListener.",
        category: "GodotUtilities.Events",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
