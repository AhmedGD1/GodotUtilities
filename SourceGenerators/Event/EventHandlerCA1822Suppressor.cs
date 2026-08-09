using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GodotUtilities.SourceGenerators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventHandlerCA1822Suppressor : DiagnosticSuppressor
{
    private const string EventHandlerAttributeFullName = "GodotUtilities.Events.EventHandlerAttribute";

    private static readonly SuppressionDescriptor Rule = new(
        id: "GUEVTSUPP001",
        suppressedDiagnosticId: "CA1822",
        justification: "Methods marked [EventHandler] must be instance methods — the generated " +
                       "WireEvents() binds a this-capturing lambda to them. See GUEVT007.");

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [Rule];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            var node = diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken)
                .FindNode(diagnostic.Location.SourceSpan);

            var methodDecl = node?.FirstAncestorOrSelf<MethodDeclarationSyntax>(ascendOutOfTrivia: true);
            if (methodDecl is null) continue;

            var semanticModel = context.GetSemanticModel(diagnostic.Location.SourceTree!);
            if (semanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken) is not IMethodSymbol methodSymbol) continue;

            if (!HasEventHandlerAttribute(methodSymbol)) continue;
            if (!IsWireable(methodSymbol.ContainingType, methodDecl)) continue;

            context.ReportSuppression(Suppression.Create(Rule, diagnostic));
        }
    }

    private static bool HasEventHandlerAttribute(IMethodSymbol methodSymbol) =>
        methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == EventHandlerAttributeFullName);

    private static bool IsWireable(INamedTypeSymbol? containingType, MethodDeclarationSyntax methodDecl)
    {
        if (containingType is null) return false;

        var isPartial = containingType.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .All(c => c.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword));
        if (!isPartial) return false;

        for (var current = containingType; current is not null; current = current.BaseType)
            if (current.ToDisplayString() == "Godot.Node") return true;
        return false;
    }
}
