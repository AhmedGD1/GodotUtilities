namespace GodotUtilities.SourceGenerators;

internal readonly record struct EventHandlerModel(
    string MethodName,
    string EventTypeFullName,
    string EventTypeDisplay,
    bool TakesParameter);

internal sealed record WireableTypeInfo(
    string Key,
    string Name,
    string DeclarationName,
    string HintName,
    string Namespace,
    bool IsPartial,
    bool DerivesFromNode,
    bool IsNested);

internal sealed record HandlerCandidate(
    WireableTypeInfo Type,
    string MethodName,
    LocationInfo? MethodLocation,
    EventHandlerModel? Handler,
    DiagnosticData? Diagnostic);

internal sealed record WireableClassModel(
    WireableTypeInfo Type,
    EquatableArray<EventHandlerModel> Handlers,
    EquatableArray<DiagnosticData> Diagnostics);
