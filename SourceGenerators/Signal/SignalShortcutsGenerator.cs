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
public sealed class SignalShortcutsGenerator : IIncrementalGenerator
{
    private const string SignalAttributeFullName = "Godot.SignalAttribute";

    private const string DelegateSuffix = "EventHandler";

    private const int MaxParameters = 9;

    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SignalAttributeFullName,
                predicate: static (node, _) => node is DelegateDeclarationSyntax,
                transform: static (ctx, ct) => Transform(ctx, ct))
            .Where(static c => c is not null)
            .Select(static (c, _) => c!);

        // Step 2: group per owning class using only plain data.
        var models = candidates
            .Collect()
            .Select(static (all, ct) => BuildModels(all, ct));

        context.RegisterSourceOutput(models, static (spc, classes) =>
        {
            foreach (var model in classes)
                Emit(spc, model);
        });
    }

    private static SignalCandidate? Transform(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol { TypeKind: TypeKind.Delegate } del)
            return null;

        if (del.ContainingType is not { TypeKind: TypeKind.Class } owner)
            return null;

        if (del.DelegateInvokeMethod is not { } invoke)
            return null;

        ct.ThrowIfCancellationRequested();

        // Godot reports all of these itself (GD0201-GD0203), so we only skip.
        if (!del.Name.EndsWith(DelegateSuffix, StringComparison.Ordinal) || del.Name.Length == DelegateSuffix.Length)
            return null;

        if (!invoke.ReturnsVoid || invoke.Parameters.Any(static p => p.RefKind != RefKind.None))
            return null;

        var signalName = del.Name.Substring(0, del.Name.Length - DelegateSuffix.Length);

        var chain = EnclosingTypes.Build(owner, out var allPartial);

        var ownerInfo = new SignalOwnerInfo(
            Key: owner.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Name: owner.Name,
            Namespace: owner.ContainingNamespace is { IsGlobalNamespace: false } ns
                ? ns.ToDisplayString()
                : string.Empty,
            EnclosingChain: chain,
            AllEnclosingPartial: allPartial,
            DerivesFromGodotObject: GodotSymbols.InheritsFromGodotObject(owner));

        var location = LocationInfo.From(((DelegateDeclarationSyntax)ctx.TargetNode).Identifier.GetLocation());

        SignalCandidate Fail(DiagnosticDescriptor descriptor, params string[] args)
            => new(ownerInfo, location, null, DiagnosticData.Create(descriptor, location, args));

        if (invoke.Parameters.Length > MaxParameters)
        {
            return Fail(
                SignalShortcutsDiagnostics.TooManyParameters,
                signalName,
                invoke.Parameters.Length.ToString(),
                MaxParameters.ToString());
        }

        foreach (var generatedName in new[]
                 {
                     "ConnectTo" + signalName,
                     "DisconnectFrom" + signalName,
                     "IsConnectedTo" + signalName,
                     "ToSignal" + signalName,
                 })
        {
            if (owner.GetMembers(generatedName).Length > 0)
                return Fail(SignalShortcutsDiagnostics.MemberNameCollision, owner.Name, generatedName, signalName);
        }

        var parameterTypes = invoke.Parameters
            .Select(static p => p.Type.ToDisplayString(TypeFormat))
            .ToArray();

        var signal = new SignalModel(
            signalName,
            ToKeyword(del.DeclaredAccessibility, owner.IsSealed),
            new EquatableArray<string>(parameterTypes));

        return new SignalCandidate(ownerInfo, location, signal, null);
    }

    private static EquatableArray<SignalClassModel> BuildModels(
        ImmutableArray<SignalCandidate> all, CancellationToken ct)
    {
        var groups = new Dictionary<string, List<SignalCandidate>>(StringComparer.Ordinal);

        foreach (var candidate in all)
        {
            ct.ThrowIfCancellationRequested();

            if (!groups.TryGetValue(candidate.Owner.Key, out var list))
                groups[candidate.Owner.Key] = list = [];
            list.Add(candidate);
        }

        var keys = groups.Keys.ToList();
        keys.Sort(StringComparer.Ordinal);

        var result = new List<SignalClassModel>(keys.Count);

        foreach (var key in keys)
        {
            var list = groups[key];
            list.Sort(static (a, b) => LocationInfo.Compare(a.Location, b.Location));

            var signals = new List<SignalModel>();
            var diagnostics = new List<DiagnosticData>();

            foreach (var candidate in list)
            {
                if (candidate.Diagnostic is not null)
                    diagnostics.Add(candidate.Diagnostic);

                if (candidate.Signal is { } signal)
                    signals.Add(signal);
            }

            result.Add(new SignalClassModel(
                list[0].Owner,
                new EquatableArray<SignalModel>(signals),
                new EquatableArray<DiagnosticData>(diagnostics)));
        }

        return new EquatableArray<SignalClassModel>(result);
    }

    private static void Emit(SourceProductionContext spc, SignalClassModel model)
    {
        var owner = model.Owner;

        if (!owner.AllEnclosingPartial || !owner.DerivesFromGodotObject)
            return;

        foreach (var diagnostic in model.Diagnostics)
            spc.ReportDiagnostic(diagnostic.ToDiagnostic());

        if (model.Signals.Count == 0)
            return;

        spc.AddSource(
            EnclosingTypes.GetHintName(owner.Namespace, owner.EnclosingChain, "SignalShortcuts.g.cs"),
            GenerateSource(model));
    }

    private static string GenerateSource(SignalClassModel model)
    {
        var owner = model.Owner;
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by GodotUtilities.SourceGenerators.SignalShortcutsGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        var hasNamespace = owner.Namespace.Length != 0;
        if (hasNamespace)
            sb.Append("namespace ").AppendLine(owner.Namespace).AppendLine("{");

        var baseIndent = hasNamespace ? "    " : "";

        var bodyIndent = EnclosingTypes.AppendOpen(sb, baseIndent, owner.EnclosingChain);

        var first = true;
        foreach (var signal in model.Signals)
        {
            if (!first) sb.AppendLine();
            first = false;
            AppendSignal(sb, bodyIndent, signal);
        }

        EnclosingTypes.AppendClose(sb, baseIndent, owner.EnclosingChain);

        if (hasNamespace)
            sb.AppendLine("}");

        return sb.ToString();
    }

    private static void AppendSignal(StringBuilder sb, string i, SignalModel signal)
    {
        var name = signal.SignalName;
        var access = signal.Accessibility;
        var action = signal.ParameterTypes.Count == 0
            ? "global::System.Action"
            : "global::System.Action<" + string.Join(", ", signal.ParameterTypes) + ">";

        // ConnectTo
        sb.Append(i).AppendLine("/// <summary>");
        sb.Append(i).Append("/// Connects <paramref name=\"handler\"/> to the <c>").Append(name).AppendLine("</c> signal.");
        sb.Append(i).AppendLine("/// </summary>");
        sb.Append(i).AppendLine("/// <param name=\"handler\">The method or lambda to call when the signal is emitted.</param>");
        sb.Append(i).AppendLine("/// <param name=\"flags\">Optional connect flags, e.g. <c>OneShot</c> or <c>Deferred</c>.</param>");
        sb.Append(i).AppendLine("/// <returns>The result of <c>Connect</c>; <c>Error.Ok</c> on success.</returns>");
        sb.Append(i).Append(access).Append(" global::Godot.Error ConnectTo").Append(name).Append('(')
          .Append(action).AppendLine(" handler, global::Godot.GodotObject.ConnectFlags flags = 0)");
        sb.Append(i).Append("    => Connect(SignalName.").Append(name)
          .AppendLine(", global::Godot.Callable.From(handler), (uint)flags);");
        sb.AppendLine();

        // DisconnectFrom
        sb.Append(i).AppendLine("/// <summary>");
        sb.Append(i).Append("/// Disconnects <paramref name=\"handler\"/> from the <c>").Append(name).AppendLine("</c> signal.");
        sb.Append(i).AppendLine("/// </summary>");
        sb.Append(i).AppendLine("/// <param name=\"handler\">The same method, or the same delegate instance, that was connected.</param>");
        sb.Append(i).AppendLine("/// <returns><c>true</c> if it was connected and has been disconnected; otherwise <c>false</c>.</returns>");
        sb.Append(i).Append(access).Append(" bool DisconnectFrom").Append(name).Append('(')
          .Append(action).AppendLine(" handler)");
        sb.Append(i).AppendLine("{");
        sb.Append(i).AppendLine("    var callable = global::Godot.Callable.From(handler);");
        sb.AppendLine();
        sb.Append(i).Append("    if (!IsConnected(SignalName.").Append(name).AppendLine(", callable))");
        sb.Append(i).AppendLine("        return false;");
        sb.AppendLine();
        sb.Append(i).Append("    Disconnect(SignalName.").Append(name).AppendLine(", callable);");
        sb.Append(i).AppendLine("    return true;");
        sb.Append(i).AppendLine("}");
        sb.AppendLine();

        // IsConnectedTo
        sb.Append(i).AppendLine("/// <summary>");
        sb.Append(i).Append("/// Whether <paramref name=\"handler\"/> is connected to the <c>").Append(name).AppendLine("</c> signal.");
        sb.Append(i).AppendLine("/// </summary>");
        sb.Append(i).AppendLine("/// <param name=\"handler\">The same method, or the same delegate instance, that was connected.</param>");
        sb.Append(i).Append(access).Append(" bool IsConnectedTo").Append(name).Append('(')
          .Append(action).AppendLine(" handler)");
        sb.Append(i).Append("    => IsConnected(SignalName.").Append(name)
          .AppendLine(", global::Godot.Callable.From(handler));");
        sb.AppendLine();

        // ToSignal
        sb.Append(i).AppendLine("/// <summary>");
        sb.Append(i).Append("/// Returns an awaiter that completes the next time the <c>").Append(name).AppendLine("</c> signal is emitted.");
        sb.Append(i).AppendLine("/// The awaited result holds the signal's arguments as a <c>Variant[]</c>.");
        sb.Append(i).AppendLine("/// </summary>");
        sb.Append(i).Append(access).Append(" global::Godot.SignalAwaiter ToSignal").Append(name).AppendLine("()");
        sb.Append(i).Append("    => ToSignal(this, SignalName.").Append(name).AppendLine(");");
    }

    private static string ToKeyword(Accessibility accessibility, bool ownerIsSealed) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Protected => ownerIsSealed ? "private" : "protected",
        Accessibility.ProtectedOrInternal => ownerIsSealed ? "internal" : "protected internal",
        Accessibility.ProtectedAndInternal => ownerIsSealed ? "private" : "private protected",
        _ => "private",
    };
}