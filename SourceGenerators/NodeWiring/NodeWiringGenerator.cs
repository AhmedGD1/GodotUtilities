using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotUtilities.SourceGenerators.NodeWiring;

[Generator(LanguageNames.CSharp)]
public sealed class NodeWiringGenerator : IIncrementalGenerator
{
    private const string NodeAttributeFullName = "GodotUtilities.NodeAttribute";
    private const string GodotNodeFullName = "Godot.Node";

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
            .Select(static (members, _) => GroupByContainingType(members));

        context.RegisterSourceOutput(grouped, static (spc, types) =>
        {
            foreach (var type in types)
            {
                Emit(spc, type);
            }
        });
    }

    private static MemberModel? Transform(GeneratorAttributeSyntaxContext ctx, System.Threading.CancellationToken ct)
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

        var godotNodeSymbol = ctx.SemanticModel.Compilation.GetTypeByMetadataName(GodotNodeFullName);

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

        bool derivesFromNode;
        bool containingDerivesFromNode;
        if (godotNodeSymbol is not null)
        {
            derivesFromNode = InheritsFrom(memberType, godotNodeSymbol);
            containingDerivesFromNode = InheritsFrom(containingType, godotNodeSymbol);
        }
        else
        {
            derivesFromNode = InheritsFromString(memberType, GodotNodeFullName);
            containingDerivesFromNode = InheritsFromString(containingType, GodotNodeFullName);
        }

        var enclosingChain = new List<EnclosingTypeInfo>();
        var allEnclosingPartial = true;
        
        for (var current = containingType; current is not null; current = current.ContainingType)
        {
            var isPartial = IsDeclaredPartial(current);
            allEnclosingPartial &= isPartial;
            enclosingChain.Add(new EnclosingTypeInfo(current.Name, GetTypeKindKeyword(current), isPartial));
        }
        
        enclosingChain.Reverse();

        return new MemberModel(
            containingType.Name,
            containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            containingType.ContainingNamespace?.IsGlobalNamespace == true
                ? null
                : containingType.ContainingNamespace?.ToDisplayString(),
            symbol.Locations.FirstOrDefault() ?? Location.None,
            allEnclosingPartial,
            containingDerivesFromNode,
            enclosingChain,
            memberName,
            memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            memberType.ToDisplayString(),
            derivesFromNode,
            isStatic,
            isProperty,
            hasAccessibleSetter,
            isInitOnly,
            explicitPath,
            hasEmptyExplicitPath,
            symbol.Locations.FirstOrDefault() ?? Location.None,
            containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            isReadOnlyField,
            isRequiredProperty
        );
    }

    private static bool InheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
        }
        return false;
    }

    private static bool InheritsFromString(ITypeSymbol type, string baseTypeFullName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + baseTypeFullName)
                return true;
        }
        return false;
    }

    private static string EscapeForStringLiteral(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string GetTypeKindKeyword(INamedTypeSymbol type) => type.TypeKind switch
    {
        TypeKind.Struct when type.IsRecord => "record struct",
        TypeKind.Struct => "struct",
        TypeKind.Class when type.IsRecord => "record",
        _ => "class",
    };

    private static bool IsDeclaredPartial(INamedTypeSymbol type)
    {
        foreach (var syntaxRef in type.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is TypeDeclarationSyntax typeDecl &&
                typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }

        return false;
    }

    private static List<TypeGroup> GroupByContainingType(ImmutableArray<MemberModel> members)
    {
        var result = new List<TypeGroup>();
        foreach (var group in members.GroupBy(m => m.ContainingTypeSymbolKey))
        {
            var list = group.ToList();
            result.Add(new TypeGroup(list[0], list));
        }

        return result;
    }

    private static void Emit(SourceProductionContext spc, TypeGroup group)
    {
        var first = group.First;
        var members = group.Members;

        if (!first.ContainingIsPartial)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ContainingTypeNotPartial,
                first.ContainingTypeLocation,
                first.ContainingTypeName,
                first.EnclosingChain.FirstOrDefault(e => !e.IsPartial)?.Name ?? first.ContainingTypeName));
            return;
        }

        if (!first.ContainingDerivesFromNode)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ContainingTypeNotNode,
                first.ContainingTypeLocation,
                first.ContainingTypeName));
            return;
        }

        var validMembers = new List<(MemberModel model, string path)>();
        var seenPaths = new Dictionary<string, MemberModel>();

        foreach (var member in members)
        {
            if (member.IsReadOnlyField)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.FieldIsReadOnly,
                    member.MemberLocation,
                    member.ContainingTypeName,
                    member.MemberName));
                continue;
            }

            if (member.IsRequiredProperty)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.PropertyIsRequired,
                    member.MemberLocation,
                    member.ContainingTypeName,
                    member.MemberName));
                continue;
            }

            if (member.IsStatic)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MemberIsStatic,
                    member.MemberLocation,
                    member.ContainingTypeName,
                    member.MemberName));
                continue;
            }

            if (!member.MemberDerivesFromNode)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MemberTypeNotNode,
                    member.MemberLocation,
                    member.ContainingTypeName,
                    member.MemberName,
                    member.MemberTypeDisplayName));
                continue;
            }

            if (member.IsProperty && !member.HasAccessibleSetter)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.PropertyHasNoSetter,
                    member.MemberLocation,
                    member.ContainingTypeName,
                    member.MemberName));
                continue;
            }

            if (member.IsProperty && member.IsInitOnly)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.PropertyIsInitOnly,
                    member.MemberLocation,
                    member.ContainingTypeName,
                    member.MemberName));
                continue;
            }

            if (member.HasEmptyExplicitPath)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.EmptyExplicitPath,
                    member.MemberLocation,
                    member.ContainingTypeName,
                    member.MemberName));
            }

            var path = member.ExplicitPath ?? NameConverter.ToNodeName(member.MemberName);

            if (seenPaths.TryGetValue(path, out _))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.DuplicateWireTarget,
                    member.MemberLocation,
                    member.ContainingTypeName,
                    member.MemberName,
                    path));
            }
            else
            {
                seenPaths[path] = member;
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
            sb.Append(levelIndent).Append("partial ").Append(level.KindKeyword).Append(' ').Append(level.Name).AppendLine();
            sb.Append(levelIndent).AppendLine("{");
        }

        var bodyIndent = baseIndent + new string(' ', first.EnclosingChain.Count * 4);

        sb.Append(bodyIndent).AppendLine("/// <summary>");
        sb.Append(bodyIndent).AppendLine("/// Resolves every [Node]-annotated member. Call this once, typically from");
        sb.Append(bodyIndent).AppendLine("/// _Ready(), before the members are used. Each member is tried, in order,");
        sb.Append(bodyIndent).AppendLine("/// against its node path (or PascalCase name), that name as a unique name");
        sb.Append(bodyIndent).AppendLine("/// (%Name), and its snake_case and camelCase forms; if none of those resolve,");
        sb.Append(bodyIndent).AppendLine("/// it falls back to a case/underscore-insensitive match against this node's");
        sb.Append(bodyIndent).AppendLine("/// direct children (looked up once, up front, rather than rescanned per member),");
        sb.Append(bodyIndent).AppendLine("/// logging a warning if the match isn't one of the member's canonical name forms,");
        sb.Append(bodyIndent).AppendLine("/// or an error if even the fallback finds nothing.");
        sb.Append(bodyIndent).AppendLine("/// </summary>");
        sb.Append(bodyIndent).AppendLine("protected void WireNodes()");
        sb.Append(bodyIndent).AppendLine("{");

        var innerIndent = bodyIndent + "    ";

        sb.Append(innerIndent).AppendLine("string __WireNodesNormalize(string s) => s.Replace(\"_\", string.Empty).ToLowerInvariant();");
        sb.Append(innerIndent).AppendLine("var __wireNodesChildren = new global::System.Collections.Generic.Dictionary<string, global::Godot.Node>();");
        sb.Append(innerIndent).AppendLine("foreach (var __child in GetChildren())");
        sb.Append(innerIndent).AppendLine("{");
        sb.Append(innerIndent).AppendLine("    var __key = __WireNodesNormalize(__child.Name.ToString());");
        sb.Append(innerIndent).AppendLine("    if (!__wireNodesChildren.ContainsKey(__key))");
        sb.Append(innerIndent).AppendLine("    {");
        sb.Append(innerIndent).AppendLine("        __wireNodesChildren[__key] = __child;");
        sb.Append(innerIndent).AppendLine("    }");
        sb.Append(innerIndent).AppendLine("}");
        sb.Append(innerIndent).AppendLine();

        sb.Append(innerIndent).AppendLine("global::Godot.Node? __WireNodesFallback(string memberName, string[] canonicalNames)");
        sb.Append(innerIndent).AppendLine("{");
        sb.Append(innerIndent).AppendLine("    var __scene = !string.IsNullOrEmpty(SceneFilePath) ? SceneFilePath : \"the scene\";");
        sb.Append(innerIndent).AppendLine("    if (!__wireNodesChildren.TryGetValue(__WireNodesNormalize(memberName), out var __match))");
        sb.Append(innerIndent).AppendLine("    {");
        sb.Append(innerIndent).AppendLine("        global::Godot.GD.PrintErr($\"WireNodes: could not match member '{memberName}' to any child node in {__scene}.\");");
        sb.Append(innerIndent).AppendLine("        return null;");
        sb.Append(innerIndent).AppendLine("    }");
        sb.Append(innerIndent).AppendLine();
        sb.Append(innerIndent).AppendLine("    if (global::System.Array.IndexOf(canonicalNames, __match.Name.ToString()) < 0)");
        sb.Append(innerIndent).AppendLine("    {");
        sb.Append(innerIndent).AppendLine("        global::Godot.GD.PushWarning($\"WireNodes: matched member '{memberName}' to node '{__match.Name}' in {__scene} as a best-guess.\");");
        sb.Append(innerIndent).AppendLine("    }");
        sb.Append(innerIndent).AppendLine();
        sb.Append(innerIndent).AppendLine("    return __match;");
        sb.Append(innerIndent).AppendLine("}");
        sb.Append(innerIndent).AppendLine();

        foreach (var (model, path) in validMembers)
        {
            var pascal = NameConverter.ToNodeName(model.MemberName);
            var snake = NameConverter.ToSnakeCase(model.MemberName);
            var camel = NameConverter.ToCamelCase(model.MemberName);

            var candidates = new List<string> { path };
            foreach (var candidate in new[] { pascal, snake, camel })
            {
                if (!candidates.Contains(candidate))
                {
                    candidates.Add(candidate);
                }
            }

            var typeName = model.MemberTypeFullyQualified;

            sb.Append(innerIndent).Append(model.MemberName).Append(" = ");
            foreach (var candidate in candidates)
            {
                var escaped = EscapeForStringLiteral(candidate);

                sb.Append("GetNodeOrNull<").Append(typeName).Append(">(\"").Append(escaped).Append("\") ?? ");

                if (candidate.IndexOf('/') < 0)
                {
                    sb.Append("GetNodeOrNull<").Append(typeName).Append(">(\"%").Append(escaped).Append("\") ?? ");
                }
            }

            sb.Append("__WireNodesFallback(\"").Append(EscapeForStringLiteral(model.MemberName)).Append("\", new[] { ");
            sb.Append(string.Join(", ", candidates.Select(c => "\"" + EscapeForStringLiteral(c) + "\"")));
            sb.Append(" }) as ").Append(typeName);
            sb.AppendLine(";");
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

        var chainNames = string.Join(".", first.EnclosingChain.Select(e => e.Name));
        var hintName = (hasNamespace ? first.ContainingNamespace + "." : "") + chainNames + ".WireNodes.g.cs";
        spc.AddSource(hintName, sb.ToString());
    }
}
