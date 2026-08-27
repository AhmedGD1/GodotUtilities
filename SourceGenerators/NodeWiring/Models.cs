using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace GodotUtilities.SourceGenerators.NodeWiring;

internal sealed record EnclosingTypeInfo(string Name, string KindKeyword, bool IsPartial);

internal sealed record MemberModel(
    string ContainingTypeName,
    string ContainingTypeFullyQualified,
    string? ContainingNamespace,
    Location ContainingTypeLocation,
    bool ContainingIsPartial,
    bool ContainingDerivesFromNode,
    IReadOnlyList<EnclosingTypeInfo> EnclosingChain,
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
    Location MemberLocation,
    string ContainingTypeSymbolKey,
    bool IsReadOnlyField = false,
    bool IsRequiredProperty = false);

internal sealed record TypeGroup(MemberModel First, IReadOnlyList<MemberModel> Members);
