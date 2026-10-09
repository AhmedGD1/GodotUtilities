using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotUtilities.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class EventHandlerGenerator : IIncrementalGenerator
{
    private const string EventHandlerAttributeFullName = "GodotUtilities.Events.EventHandlerAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: one small, value-equatable model per [EventHandler] method. Roslyn caches this per
        // method, so editing an unrelated file doesn't re-run it.
        var candidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                EventHandlerAttributeFullName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, ct) => Transform(ctx, ct))
            .Where(static c => c is not null)
            .Select(static (c, _) => c!);

        // Step 2: group per class using only plain data. The result compares by value, so when no
        // handler changed, the output step below is skipped entirely.
        var models = candidates
            .Collect()
            .Select(static (all, ct) => BuildModels(all, ct));

        context.RegisterSourceOutput(models, static (spc, classes) =>
        {
            foreach (var model in classes)
                Emit(spc, model);
        });
    }

    private static HandlerCandidate? Transform(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not IMethodSymbol method) return null;

        var classSymbol = method.ContainingType;
        if (classSymbol is null || classSymbol.TypeKind != TypeKind.Class) return null;

        var attribute = ctx.Attributes.FirstOrDefault();
        if (attribute is null) return null;

        ct.ThrowIfCancellationRequested();

        var type = new WireableTypeInfo(
            Key: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Name: classSymbol.Name,
            DeclarationName: GodotSymbols.GetDeclarationName(classSymbol),
            HintName: GodotSymbols.GetHintName(classSymbol),
            Namespace: classSymbol.ContainingNamespace is { IsGlobalNamespace: false } ns
                ? ns.ToDisplayString()
                : string.Empty,
            IsPartial: GodotSymbols.IsDeclaredPartial(classSymbol),
            DerivesFromNode: GodotSymbols.InheritsFromGodotNode(classSymbol),
            IsNested: classSymbol.ContainingType is not null);

        var location = LocationInfo.From(((MethodDeclarationSyntax)ctx.TargetNode).Identifier.GetLocation());

        HandlerCandidate Fail(DiagnosticDescriptor descriptor, params string[] args)
            => new(type, method.Name, location, null, DiagnosticData.Create(descriptor, location, args));

        if (method.IsStatic)
            return Fail(EventHandlerDiagnostics.StaticMethodNotSupported, method.Name);

        var parameters = method.Parameters;
        if (parameters.Length > 1)
            return Fail(EventHandlerDiagnostics.TooManyParameters, method.Name, parameters.Length.ToString());

        ITypeSymbol? explicitType = null;
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is ITypeSymbol typeArg)
        {
            explicitType = typeArg;
        }

        var parameterType = parameters.Length > 0 ? parameters[0].Type : null;
        var eventType = explicitType ?? parameterType;

        if (eventType is null)
            return Fail(EventHandlerDiagnostics.MissingEventType, method.Name);

        if (explicitType is not null && parameterType is not null &&
            !SymbolEqualityComparer.Default.Equals(explicitType, parameterType))
        {
            return Fail(
                EventHandlerDiagnostics.ParameterTypeMismatch,
                method.Name,
                explicitType.ToDisplayString(),
                parameterType.ToDisplayString());
        }

        var handler = new EventHandlerModel(
            method.Name,
            eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            eventType.ToDisplayString(),
            parameters.Length > 0);

        return new HandlerCandidate(type, method.Name, location, handler, null);
    }

    private static EquatableArray<WireableClassModel> BuildModels(
        ImmutableArray<HandlerCandidate> all, CancellationToken ct)
    {
        var groups = new Dictionary<string, List<HandlerCandidate>>(StringComparer.Ordinal);

        foreach (var candidate in all)
        {
            ct.ThrowIfCancellationRequested();

            if (!groups.TryGetValue(candidate.Type.Key, out var list))
                groups[candidate.Type.Key] = list = [];
            list.Add(candidate);
        }

        // Deterministic order regardless of which file Roslyn happened to hand us first.
        var keys = groups.Keys.ToList();
        keys.Sort(StringComparer.Ordinal);

        var result = new List<WireableClassModel>(keys.Count);

        foreach (var key in keys)
        {
            var list = groups[key];
            list.Sort(static (a, b) => LocationInfo.Compare(a.MethodLocation, b.MethodLocation));

            var type = list[0].Type;
            var firstLocation = list[0].MethodLocation;
            var diagnostics = new List<DiagnosticData>();

            if (type.IsNested)
                diagnostics.Add(DiagnosticData.Create(EventHandlerDiagnostics.NestedClassNotSupported, firstLocation, type.Name));
            if (!type.IsPartial)
                diagnostics.Add(DiagnosticData.Create(EventHandlerDiagnostics.ContainingClassNotPartial, firstLocation, type.Name));
            if (!type.DerivesFromNode)
                diagnostics.Add(DiagnosticData.Create(EventHandlerDiagnostics.ContainingClassNotNode, firstLocation, type.Name));

            var handlers = new List<EventHandlerModel>();
            var seenEventTypes = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var candidate in list)
            {
                if (candidate.Diagnostic is not null)
                    diagnostics.Add(candidate.Diagnostic);

                if (candidate.Handler is not { } handler)
                    continue;

                if (seenEventTypes.TryGetValue(handler.EventTypeFullName, out var firstMethodName))
                {
                    diagnostics.Add(DiagnosticData.Create(
                        EventHandlerDiagnostics.DuplicateHandlerForType,
                        candidate.MethodLocation,
                        type.Name,
                        handler.EventTypeDisplay,
                        firstMethodName,
                        handler.MethodName));
                    continue;
                }

                seenEventTypes[handler.EventTypeFullName] = handler.MethodName;
                handlers.Add(handler);
            }

            result.Add(new WireableClassModel(
                type,
                new EquatableArray<EventHandlerModel>(handlers),
                new EquatableArray<DiagnosticData>(diagnostics)));
        }

        return new EquatableArray<WireableClassModel>(result);
    }

    private static void Emit(SourceProductionContext spc, WireableClassModel model)
    {
        foreach (var diagnostic in model.Diagnostics)
            spc.ReportDiagnostic(diagnostic.ToDiagnostic());

        var type = model.Type;

        if (model.Handlers.Count == 0) return;
        if (!type.IsPartial || !type.DerivesFromNode || type.IsNested) return;

        var hintPrefix = string.IsNullOrEmpty(type.Namespace)
            ? type.HintName
            : $"{type.Namespace}.{type.HintName}";

        spc.AddSource($"{hintPrefix}.EventHandlers.g.cs", GenerateSource(model));
    }

    private static string GenerateSource(WireableClassModel model)
    {
        var type = model.Type;
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by GodotUtilities.SourceGenerators.EventHandlerGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        var hasNamespace = !string.IsNullOrEmpty(type.Namespace);
        if (hasNamespace)
        {
            sb.Append("namespace ").Append(type.Namespace).AppendLine();
            sb.AppendLine("{");
        }

        var indent = hasNamespace ? "    " : "";

        sb.Append(indent).Append("partial class ").Append(type.DeclarationName).AppendLine();
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    /// <summary>");
        sb.Append(indent).AppendLine("    /// Subscribes every [EventHandler] method on this class to the EventBus.");
        sb.Append(indent).AppendLine("    /// Generated at compile time - no reflection involved. Call from _EnterTree()");
        sb.Append(indent).AppendLine("    /// (or NotificationEnterTree) so subscriptions are restored if the node is reparented;");
        sb.Append(indent).AppendLine("    /// calling it from _Ready() works too but won't re-subscribe after a reparent.");
        sb.Append(indent).AppendLine("    /// Subscriptions are removed automatically when the node leaves the tree.");
        sb.Append(indent).AppendLine("    /// </summary>");
        sb.Append(indent).AppendLine("    public void WireEvents()");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        if (!global::GodotUtilities.Events.EventBus.TryBeginWiring(this))");
        sb.Append(indent).AppendLine("            return;");
        sb.AppendLine();

        foreach (var handler in model.Handlers)
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
