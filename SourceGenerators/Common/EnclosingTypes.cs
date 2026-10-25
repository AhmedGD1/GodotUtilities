using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace GodotUtilities.SourceGenerators;

/// <summary>One level of a type's declaration chain (a class and the types it is nested in). Plain data, so Roslyn can cache by value.</summary>
internal sealed record EnclosingTypeInfo(
    string Name,
    string DeclarationName,
    string HintName,
    string KindKeyword,
    bool IsPartial);

/// <summary>
/// Shared by the generators that add members to a (possibly nested) partial class:
/// builds the chain of enclosing types and re-declares it around the generated code.
/// </summary>
internal static class EnclosingTypes
{
    /// <summary>The chain for <paramref name="type"/>: outermost type first, <paramref name="type"/> itself last.</summary>
    /// <param name="allPartial">Whether <paramref name="type"/> and every type around it is declared partial.</param>
    public static EquatableArray<EnclosingTypeInfo> Build(INamedTypeSymbol type, out bool allPartial)
    {
        var chain = new List<EnclosingTypeInfo>();
        allPartial = true;

        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            var isPartial = GodotSymbols.IsDeclaredPartial(current);
            allPartial &= isPartial;

            chain.Add(new EnclosingTypeInfo(
                current.Name,
                GodotSymbols.GetDeclarationName(current),
                GodotSymbols.GetHintName(current),
                GodotSymbols.GetTypeKindKeyword(current),
                isPartial));
        }

        chain.Reverse();
        return new EquatableArray<EnclosingTypeInfo>(chain);
    }

    /// <summary>
    /// Writes one <c>partial</c> declaration (and its opening brace) per level of <paramref name="chain"/>.
    /// </summary>
    /// <returns>The indentation to use for the members inside the innermost type.</returns>
    public static string AppendOpen(StringBuilder sb, string baseIndent, EquatableArray<EnclosingTypeInfo> chain)
    {
        for (var i = 0; i < chain.Count; i++)
        {
            var level = chain[i];
            var levelIndent = baseIndent + new string(' ', i * 4);

            sb.Append(levelIndent).Append("partial ").Append(level.KindKeyword).Append(' ').AppendLine(level.DeclarationName);
            sb.Append(levelIndent).AppendLine("{");
        }

        return baseIndent + new string(' ', chain.Count * 4);
    }

    /// <summary>Writes the closing braces for what <see cref="AppendOpen"/> opened.</summary>
    public static void AppendClose(StringBuilder sb, string baseIndent, EquatableArray<EnclosingTypeInfo> chain)
    {
        for (var i = chain.Count - 1; i >= 0; i--)
            sb.Append(baseIndent).Append(new string(' ', i * 4)).AppendLine("}");
    }

    /// <summary>
    /// A hint name that is unique per type: <c>Namespace.Outer+Inner.{suffix}</c>.
    /// Nested types are joined with '+' (never valid in an identifier), so a nested <c>A.B</c> in namespace
    /// <c>N</c> can't collide with a top-level <c>B</c> in namespace <c>N.A</c>.
    /// </summary>
    public static string GetHintName(string? @namespace, EquatableArray<EnclosingTypeInfo> chain, string suffix)
    {
        var names = string.Join("+", chain.Select(static e => e.HintName));
        return (string.IsNullOrEmpty(@namespace) ? "" : @namespace + ".") + names + "." + suffix;
    }
}
