namespace GodotUtilities.SourceGenerators;

internal enum SingletonKind
{
    Scene = 0,
    Autoload = 1,
    Lazy = 2,
}

internal sealed record SingletonModel(
    string Name,
    string Namespace,
    EquatableArray<EnclosingTypeInfo> EnclosingChain,
    SingletonKind Kind,
    string NodeName,
    string? ScenePath,
    bool IsNode,
    bool IsGodotObject,
    EquatableArray<DiagnosticData> Diagnostics,
    bool CanGenerate);
