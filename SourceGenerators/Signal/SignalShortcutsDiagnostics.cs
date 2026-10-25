using Microsoft.CodeAnalysis;

namespace GodotUtilities.SourceGenerators;

internal static class SignalShortcutsDiagnostics
{
    public static readonly DiagnosticDescriptor TooManyParameters = new(
        id: "GUSIG001",
        title: "Signal has too many parameters for shortcut methods",
        messageFormat: "Signal '{0}' has {1} parameters, but shortcut methods support at most {2} (the limit of Callable.From). No ConnectTo/DisconnectFrom/IsConnectedTo/ToSignal methods were generated for it.",
        category: "GodotUtilities.Signals",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MemberNameCollision = new(
        id: "GUSIG002",
        title: "Signal shortcut method name is already in use",
        messageFormat: "Class '{0}' already declares a member named '{1}', so the shortcut methods for signal '{2}' were not generated. Rename the member or the signal.",
        category: "GodotUtilities.Signals",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
