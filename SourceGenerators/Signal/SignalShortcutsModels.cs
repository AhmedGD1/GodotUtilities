namespace GodotUtilities.SourceGenerators;

internal sealed record SignalOwnerInfo(
    string Key,
    string Name,
    string Namespace,
    EquatableArray<EnclosingTypeInfo> EnclosingChain,
    bool AllEnclosingPartial,
    bool DerivesFromGodotObject);

internal readonly record struct SignalModel(
    string SignalName,
    string Accessibility,
    EquatableArray<string> ParameterTypes);

internal sealed record SignalCandidate(
    SignalOwnerInfo Owner,
    LocationInfo? Location,
    SignalModel? Signal,
    DiagnosticData? Diagnostic);

internal sealed record SignalClassModel(
    SignalOwnerInfo Owner,
    EquatableArray<SignalModel> Signals,
    EquatableArray<DiagnosticData> Diagnostics);