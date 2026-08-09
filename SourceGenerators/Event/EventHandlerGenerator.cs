using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotUtilities.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class EventHandlerGenerator : IIncrementalGenerator
{
    private const string EventHandlerAttributeFullName = "GodotUtilities.Events.EventHandlerAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidateMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                EventHandlerAttributeFullName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => (MethodDeclarationSyntax)ctx.TargetNode)
            .Collect();

        var classModels = candidateMethods
            .Combine(context.CompilationProvider)
            .SelectMany(static (pair, ct) =>
            {
                var (methods, compilation) = pair;
                var results = new List<(WireableClassModel Model, ImmutableArray<Diagnostic> Diagnostics)>();

                var byClass = new Dictionary<INamedTypeSymbol, List<MethodDeclarationSyntax>>(SymbolEqualityComparer.Default);
                foreach (var method in methods)
                {
                    ct.ThrowIfCancellationRequested();
                    if (method.Parent is not ClassDeclarationSyntax classDecl) continue;

                    var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                    if (classSymbol is null) continue;

                    if (!byClass.TryGetValue(classSymbol, out var list))
                        byClass[classSymbol] = list = [];
                    list.Add(method);
                }

                foreach (var entry in byClass)
                {
                    ct.ThrowIfCancellationRequested();
                    results.Add(BuildModel(entry.Key, entry.Value, compilation));
                }

                return results;
            });

        context.RegisterSourceOutput(classModels, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
                spc.ReportDiagnostic(diagnostic);

            if (result.Model.Handlers.Items.Count == 0) return;
            if (!result.Model.IsPartial || !result.Model.DerivesFromNode || result.Model.IsNested) return;

            var hintPrefix = string.IsNullOrEmpty(result.Model.Namespace)
                ? result.Model.ClassName
                : $"{result.Model.Namespace}.{result.Model.ClassName}";

            spc.AddSource($"{hintPrefix}.EventHandlers.g.cs", GenerateSource(result.Model));
        });
    }

    private static (WireableClassModel, ImmutableArray<Diagnostic>) BuildModel(
        INamedTypeSymbol classSymbol, List<MethodDeclarationSyntax> methodDecls, Compilation compilation)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var className = classSymbol.Name;
        var namespaceName = classSymbol.ContainingNamespace is { IsGlobalNamespace: false } ns
            ? ns.ToDisplayString()
            : string.Empty;

        var isPartial = classSymbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .All(c => c.Modifiers.Any(SyntaxKind.PartialKeyword));

        var derivesFromNode = DerivesFromGodotNode(classSymbol);
        var isNested = classSymbol.ContainingType is not null;
        var firstDecl = methodDecls[0];

        if (isNested)
        {
            diagnostics.Add(Diagnostic.Create(
                EventHandlerDiagnostics.NestedClassNotSupported,
                firstDecl.Identifier.GetLocation(),
                className));
        }

        if (!isPartial)
        {
            diagnostics.Add(Diagnostic.Create(
                EventHandlerDiagnostics.ContainingClassNotPartial,
                firstDecl.Identifier.GetLocation(),
                className));
        }

        if (!derivesFromNode)
        {
            diagnostics.Add(Diagnostic.Create(
                EventHandlerDiagnostics.ContainingClassNotNode,
                firstDecl.Identifier.GetLocation(),
                className));
        }

        var handlers = new List<EventHandlerModel>();
        var seenEventTypes = new Dictionary<string, string>();

        foreach (var member in methodDecls)
        {
            var semanticModel = compilation.GetSemanticModel(member.SyntaxTree);
            var methodSymbol = semanticModel.GetDeclaredSymbol(member);
            if (methodSymbol is null) continue;

            var attributeData = methodSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == EventHandlerAttributeFullName);
            if (attributeData is null) continue;

            if (methodSymbol.IsStatic)
            {
                diagnostics.Add(Diagnostic.Create(
                    EventHandlerDiagnostics.StaticMethodNotSupported,
                    member.Identifier.GetLocation(),
                    methodSymbol.Name));
                continue;
            }

            var parameters = methodSymbol.Parameters;
            if (parameters.Length > 1)
            {
                diagnostics.Add(Diagnostic.Create(
                    EventHandlerDiagnostics.TooManyParameters,
                    member.Identifier.GetLocation(),
                    methodSymbol.Name,
                    parameters.Length));
                continue;
            }

            ITypeSymbol? explicitType = null;
            if (attributeData.ConstructorArguments.Length > 0 &&
                attributeData.ConstructorArguments[0].Value is ITypeSymbol typeArg)
            {
                explicitType = typeArg;
            }

            var parameterType = parameters.Length > 0 ? parameters[0].Type : null;
            var eventType = explicitType ?? parameterType;

            if (eventType is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    EventHandlerDiagnostics.MissingEventType,
                    member.Identifier.GetLocation(),
                    methodSymbol.Name));
                continue;
            }

            if (explicitType is not null && parameterType is not null &&
                !SymbolEqualityComparer.Default.Equals(explicitType, parameterType))
            {
                diagnostics.Add(Diagnostic.Create(
                    EventHandlerDiagnostics.ParameterTypeMismatch,
                    member.Identifier.GetLocation(),
                    methodSymbol.Name,
                    explicitType.ToDisplayString(),
                    parameterType.ToDisplayString()));
                continue;
            }

            var eventTypeFullName = eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (seenEventTypes.TryGetValue(eventTypeFullName, out var firstMethodName))
            {
                diagnostics.Add(Diagnostic.Create(
                    EventHandlerDiagnostics.DuplicateHandlerForType,
                    member.Identifier.GetLocation(),
                    className,
                    eventType.ToDisplayString(),
                    firstMethodName,
                    methodSymbol.Name));
                continue;
            }
            seenEventTypes[eventTypeFullName] = methodSymbol.Name;

            handlers.Add(new EventHandlerModel(methodSymbol.Name, eventTypeFullName, parameters.Length > 0));
        }

        var model = new WireableClassModel
        {
            ClassName = className,
            Namespace = namespaceName,
            IsPartial = isPartial,
            DerivesFromNode = derivesFromNode,
            IsNested = isNested,
            FilePathHint = firstDecl.SyntaxTree.FilePath,
            Handlers = new EquatableArray<EventHandlerModel>(handlers),
        };

        return (model, diagnostics.ToImmutable());
    }

    private static bool DerivesFromGodotNode(INamedTypeSymbol symbol)
    {
        for (var current = symbol; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "Godot.Node") return true;
        }
        return false;
    }

    private static string GenerateSource(WireableClassModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by GodotUtilities.Events.SourceGenerators.EventHandlerGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        var hasNamespace = !string.IsNullOrEmpty(model.Namespace);
        if (hasNamespace)
        {
            sb.Append("namespace ").Append(model.Namespace).AppendLine();
            sb.AppendLine("{");
        }

        var indent = hasNamespace ? "    " : "";

        sb.Append(indent).Append("partial class ").Append(model.ClassName).AppendLine();
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    /// <summary>");
        sb.Append(indent).AppendLine("    /// Subscribes every [EventHandler] method on this class to the EventBus.");
        sb.Append(indent).AppendLine("    /// Generated at compile time — no reflection involved. Call once, typically");
        sb.Append(indent).AppendLine("    /// from _Ready(). Automatically unsubscribed when this node leaves the tree.");
        sb.Append(indent).AppendLine("    /// </summary>");
        sb.Append(indent).AppendLine("    public void WireEvents()");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        if (!global::GodotUtilities.Events.EventBus.TryBeginWiring(this))");
        sb.Append(indent).AppendLine("            return;");
        sb.AppendLine();

        foreach (var handler in model.Handlers.Items)
        {
            var lambda = handler.TakesParameter
                ? $"({handler.EventTypeFullName} evt) => {handler.MethodName}(evt)"
                : $"({handler.EventTypeFullName} _) => {handler.MethodName}()";

            sb.Append(indent).Append("        global::GodotUtilities.Events.EventBus.AddListener<")
              .Append(handler.EventTypeFullName).Append(">(").Append(lambda).Append(", this);")
              .AppendLine();
        }

        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("}");

        if (hasNamespace)
            sb.AppendLine("}");

        return sb.ToString();
    }
}
