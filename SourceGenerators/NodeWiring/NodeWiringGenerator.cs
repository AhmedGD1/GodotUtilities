using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotUtilities.SourceGenerators.NodeWiring;

[Generator(LanguageNames.CSharp)]
public sealed class NodeWiringGenerator : IIncrementalGenerator
{
    private const string NodeAttributeFullName = "GodotUtilities.NodeAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                NodeAttributeFullName,
                predicate: static (node, _) => node is VariableDeclaratorSyntax or PropertyDeclarationSyntax,
                transform: static (ctx, ct) => Transform(ctx, ct))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        var grouped = candidates
            .Collect()
            .Select(static (members, ct) => GroupByContainingType(members, ct));

        context.RegisterSourceOutput(grouped, static (spc, types) =>
        {
            foreach (var type in types)
            {
                Emit(spc, type);
            }
        });
    }

    private static MemberModel? Transform(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        var symbol = ctx.TargetSymbol;
        var attributeData = ctx.Attributes.FirstOrDefault();
        if (attributeData is null)
        {
            return null;
        }

        var containingType = symbol.ContainingType;
        if (containingType is null)
        {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        string memberName;
        ITypeSymbol memberType;
        bool isStatic;
        bool isProperty;
        bool hasAccessibleSetter = true;
        bool isInitOnly = false;
        bool isReadOnlyField = false;
        bool isRequiredProperty = false;

        switch (symbol)
        {
            case IFieldSymbol field:
                memberName = field.Name;
                memberType = field.Type;
                isStatic = field.IsStatic;
                isProperty = false;
                isReadOnlyField = field.IsReadOnly;
                break;

            case IPropertySymbol property:
                memberName = property.Name;
                memberType = property.Type;
                isStatic = property.IsStatic;
                isProperty = true;
                hasAccessibleSetter = property.SetMethod is not null;
                isInitOnly = property.SetMethod?.IsInitOnly ?? false;
                isRequiredProperty = property.IsRequired;
                break;

            default:
                return null;
        }

        string? explicitPath = null;
        var hasEmptyExplicitPath = false;
        var ctorArgs = attributeData.ConstructorArguments;
        if (ctorArgs.Length > 0 && ctorArgs[0].Value is string pathArg)
        {
            if (string.IsNullOrWhiteSpace(pathArg))
            {
                hasEmptyExplicitPath = true;
            }
            else
            {
                explicitPath = pathArg;
            }
        }

        var chain = new List<EnclosingTypeInfo>();
        var allEnclosingPartial = true;

        for (var current = containingType; current is not null; current = current.ContainingType)
        {
            var isPartial = GodotSymbols.IsDeclaredPartial(current);
            allEnclosingPartial &= isPartial;
            chain.Add(new EnclosingTypeInfo(
                current.Name,
                GodotSymbols.GetDeclarationName(current),
                GodotSymbols.GetHintName(current),
                GetTypeKindKeyword(current),
                isPartial));
        }

        chain.Reverse();

        return new MemberModel(
            ContainingTypeName: containingType.Name,
            ContainingTypeKey: containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ContainingNamespace: containingType.ContainingNamespace is { IsGlobalNamespace: false } ns
                ? ns.ToDisplayString()
                : null,
            ContainingTypeLocation: LocationInfo.From(containingType.Locations.FirstOrDefault()),
            ContainingIsPartial: allEnclosingPartial,
            ContainingDerivesFromNode: GodotSymbols.InheritsFromGodotNode(containingType),
            EnclosingChain: new EquatableArray<EnclosingTypeInfo>(chain),
            MemberName: memberName,
            MemberTypeFullyQualified: memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).TrimEnd('?'),
            MemberTypeDisplayName: memberType.ToDisplayString(),
            MemberDerivesFromNode: GodotSymbols.InheritsFromGodotNode(memberType),
            IsStatic: isStatic,
            IsProperty: isProperty,
            HasAccessibleSetter: hasAccessibleSetter,
            IsInitOnly: isInitOnly,
            ExplicitPath: explicitPath,
            HasEmptyExplicitPath: hasEmptyExplicitPath,
            MemberLocation: LocationInfo.From(symbol.Locations.FirstOrDefault()),
            IsReadOnlyField: isReadOnlyField,
            IsRequiredProperty: isRequiredProperty);
    }

    private static string EscapeForStringLiteral(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string GetTypeKindKeyword(INamedTypeSymbol type) => type.TypeKind switch
    {
        TypeKind.Struct when type.IsRecord => "record struct",
        TypeKind.Struct => "struct",
        TypeKind.Class when type.IsRecord => "record",
        _ => "class",
    };

    private static EquatableArray<TypeGroup> GroupByContainingType(
        ImmutableArray<MemberModel> members, CancellationToken ct)
    {
        var groups = new Dictionary<string, List<MemberModel>>(StringComparer.Ordinal);

        foreach (var member in members)
        {
            ct.ThrowIfCancellationRequested();

            if (!groups.TryGetValue(member.ContainingTypeKey, out var list))
            {
                groups[member.ContainingTypeKey] = list = [];
            }

            list.Add(member);
        }

        // Deterministic ordering, independent of the order Roslyn enumerates files in.
        var keys = groups.Keys.ToList();
        keys.Sort(StringComparer.Ordinal);

        var result = new List<TypeGroup>(keys.Count);
        foreach (var key in keys)
        {
            var list = groups[key];
            list.Sort(static (a, b) => LocationInfo.Compare(a.MemberLocation, b.MemberLocation));
            result.Add(new TypeGroup(list[0], new EquatableArray<MemberModel>(list)));
        }

        return new EquatableArray<TypeGroup>(result);
    }

    private static void Emit(SourceProductionContext spc, TypeGroup group)
    {
        var first = group.First;

        if (!first.ContainingIsPartial)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ContainingTypeNotPartial,
                first.ContainingTypeLocation.ToLocationOrNone(),
                first.ContainingTypeName,
                first.EnclosingChain.FirstOrDefault(e => !e.IsPartial)?.Name ?? first.ContainingTypeName));
            return;
        }

        if (!first.ContainingDerivesFromNode)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ContainingTypeNotNode,
                first.ContainingTypeLocation.ToLocationOrNone(),
                first.ContainingTypeName));
            return;
        }

        var validMembers = new List<(MemberModel model, string path)>();
        var seenPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in group.Members)
        {
            var location = member.MemberLocation.ToLocationOrNone();

            if (member.IsReadOnlyField)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.FieldIsReadOnly, location, member.ContainingTypeName, member.MemberName));
                continue;
            }

            if (member.IsRequiredProperty)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.PropertyIsRequired, location, member.ContainingTypeName, member.MemberName));
                continue;
            }

            if (member.IsStatic)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MemberIsStatic, location, member.ContainingTypeName, member.MemberName));
                continue;
            }

            if (!member.MemberDerivesFromNode)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MemberTypeNotNode, location,
                    member.ContainingTypeName, member.MemberName, member.MemberTypeDisplayName));
                continue;
            }

            if (member.IsProperty && !member.HasAccessibleSetter)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.PropertyHasNoSetter, location, member.ContainingTypeName, member.MemberName));
                continue;
            }

            if (member.IsProperty && member.IsInitOnly)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.PropertyIsInitOnly, location, member.ContainingTypeName, member.MemberName));
                continue;
            }

            if (member.HasEmptyExplicitPath)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.EmptyExplicitPath, location, member.ContainingTypeName, member.MemberName));
            }

            var path = member.ExplicitPath ?? NameConverter.ToNodeName(member.MemberName);

            if (!seenPaths.Add(path))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.DuplicateWireTarget, location,
                    member.ContainingTypeName, member.MemberName, path));
            }

            validMembers.Add((member, path));
        }

        if (validMembers.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS8600, CS8601");
        sb.AppendLine();

        var hasNamespace = !string.IsNullOrEmpty(first.ContainingNamespace);
        if (hasNamespace)
        {
            sb.Append("namespace ").Append(first.ContainingNamespace).AppendLine();
            sb.AppendLine("{");
        }

        var baseIndent = hasNamespace ? "    " : "";
        for (var i = 0; i < first.EnclosingChain.Count; i++)
        {
            var level = first.EnclosingChain[i];
            var levelIndent = baseIndent + new string(' ', i * 4);
            sb.Append(levelIndent).Append("partial ").Append(level.KindKeyword).Append(' ').Append(level.DeclarationName).AppendLine();
            sb.Append(levelIndent).AppendLine("{");
        }

        var bodyIndent = baseIndent + new string(' ', first.EnclosingChain.Count * 4);
        var innerIndent = bodyIndent + "    ";

        AppendBlock(sb, bodyIndent, WireNodesPrologue);

        foreach (var (model, _) in validMembers)
        {
            sb.Append(innerIndent).AppendLine(BuildAssignment(model));
        }

        sb.Append(bodyIndent).AppendLine("}");

        for (var i = first.EnclosingChain.Count - 1; i >= 0; i--)
        {
            var levelIndent = baseIndent + new string(' ', i * 4);
            sb.Append(levelIndent).AppendLine("}");
        }

        if (hasNamespace)
        {
            sb.AppendLine("}");
        }

        var chainNames = string.Join(".", first.EnclosingChain.Select(e => e.HintName));
        var hintName = (hasNamespace ? first.ContainingNamespace + "." : "") + chainNames + ".WireNodes.g.cs";
        spc.AddSource(hintName, sb.ToString());
    }

    private static string BuildAssignment(MemberModel model)
    {
        var pascal = NameConverter.ToNodeName(model.MemberName);
        var raw = model.MemberName.TrimStart('_');
        var snake = NameConverter.ToSnakeCase(model.MemberName);
        var camel = NameConverter.ToCamelCase(model.MemberName);

        var candidates = new List<string>();
        foreach (var candidate in new[] { model.ExplicitPath, pascal, raw, snake, camel })
        {
            if (!string.IsNullOrEmpty(candidate) && !candidates.Contains(candidate!))
            {
                candidates.Add(candidate!);
            }
        }

        var typeName = model.MemberTypeFullyQualified;
        var sb = new StringBuilder();
        sb.Append(model.MemberName).Append(" = ");

        foreach (var candidate in candidates)
        {
            var escaped = EscapeForStringLiteral(candidate);

            sb.Append("GetNodeOrNull<").Append(typeName).Append(">(\"").Append(escaped).Append("\") ?? ");

            if (candidate.IndexOf('/') < 0)
            {
                sb.Append("GetNodeOrNull<").Append(typeName).Append(">(\"%").Append(escaped).Append("\") ?? ");
            }
        }

        sb.Append("__WireNodesFallback<").Append(typeName).Append(">(\"")
          .Append(EscapeForStringLiteral(model.MemberName)).Append("\", new[] { ")
          .Append(string.Join(", ", candidates.Select(c => "\"" + EscapeForStringLiteral(c) + "\"")))
          .Append(" });");

        return sb.ToString();
    }

    private static void AppendBlock(StringBuilder sb, string indent, string block)
    {
        foreach (var line in block.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.Length == 0)
            {
                sb.AppendLine();
            }
            else
            {
                sb.Append(indent).AppendLine(line);
            }
        }
    }

    private const string WireNodesPrologue = """
        /// <summary>
        /// Resolves every [Node]-annotated member. Call this once, typically from _Ready() (or on
        /// NotificationSceneInstantiated), before the members are used. Each member is tried against
        /// its explicit path, then its PascalCase / exact / snake_case / camelCase names, each as a
        /// normal path and as a unique name (%Name). If none resolve, it falls back to a case- and
        /// underscore-insensitive match against this node's direct children (built lazily, only if
        /// needed), logging a warning for a best-guess match, or an error if the match is missing
        /// or is the wrong type.
        /// </summary>
        protected void WireNodes()
        {
            global::System.Collections.Generic.Dictionary<string, global::Godot.Node>? __wireNodesChildren = null;

            static string __WireNodesNormalize(string s) => s.Replace("_", string.Empty).ToLowerInvariant();

            T? __WireNodesFallback<T>(string memberName, string[] canonicalNames) where T : global::Godot.Node
            {
                var __scene = !string.IsNullOrEmpty(SceneFilePath) ? SceneFilePath : "the scene";

                if (__wireNodesChildren is null)
                {
                    __wireNodesChildren = new global::System.Collections.Generic.Dictionary<string, global::Godot.Node>();
                    foreach (var __child in GetChildren())
                    {
                        var __key = __WireNodesNormalize(__child.Name.ToString());
                        if (!__wireNodesChildren.ContainsKey(__key))
                        {
                            __wireNodesChildren[__key] = __child;
                        }
                    }
                }

                if (!__wireNodesChildren.TryGetValue(__WireNodesNormalize(memberName), out var __match))
                {
                    global::Godot.GD.PrintErr($"WireNodes: could not match member '{memberName}' to any child node in {__scene}.");
                    return null;
                }

                if (__match is not T __typed)
                {
                    global::Godot.GD.PushError($"WireNodes: child '{__match.Name}' matches member '{memberName}' in {__scene}, but it is a {__match.GetType().Name}, not a {typeof(T).Name}.");
                    return null;
                }

                if (global::System.Array.IndexOf(canonicalNames, __match.Name.ToString()) < 0)
                {
                    global::Godot.GD.PushWarning($"WireNodes: matched member '{memberName}' to node '{__match.Name}' in {__scene} as a best-guess.");
                }

                return __typed;
            }

        """;
}
