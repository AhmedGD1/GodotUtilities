using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotUtilities.SourceGenerators;

internal static class GodotSymbols
{
    public static bool InheritsFromGodotNode(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "Node"
                && current.ContainingNamespace is { IsGlobalNamespace: false } ns
                && ns.Name == "Godot"
                && ns.ContainingNamespace is { IsGlobalNamespace: true })
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsDeclaredPartial(INamedTypeSymbol type)
    {
        var sawDeclaration = false;

        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is not TypeDeclarationSyntax declaration)
                continue;

            sawDeclaration = true;
            if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                return false;
        }

        return sawDeclaration;
    }

    public static string GetDeclarationName(INamedTypeSymbol type)
        => type.TypeParameters.Length == 0
            ? type.Name
            : type.Name + "<" + string.Join(", ", type.TypeParameters.Select(p => p.Name)) + ">";

    public static string GetHintName(INamedTypeSymbol type)
        => type.Arity == 0 ? type.Name : type.Name + "_G" + type.Arity;
}
