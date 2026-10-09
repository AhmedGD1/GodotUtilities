namespace GodotUtilities.SourceGenerators.NodeWiring;

internal sealed record EnclosingTypeInfo(
    string Name,
    string DeclarationName,
    string HintName,
    string KindKeyword,
    bool IsPartial);

internal sealed record MemberModel(
    string ContainingTypeName,
    string ContainingTypeKey,
    string? ContainingNamespace,
    LocationInfo? ContainingTypeLocation,
    bool ContainingIsPartial,
    bool ContainingDerivesFromNode,
    EquatableArray<EnclosingTypeInfo> EnclosingChain,
    string MemberName,
    string MemberTypeFullyQualified,
    string MemberTypeDisplayName,
    bool MemberDerivesFromNode,
    bool IsStatic,
    bool IsProperty,
    bool HasAccessibleSetter,
    bool IsInitOnly,
    string? ExplicitPath,
    bool HasEmptyExplicitPath,
    LocationInfo? MemberLocation,
    bool IsReadOnlyField,
    bool IsRequiredProperty);

internal sealed record TypeGroup(MemberModel First, EquatableArray<MemberModel> Members);
