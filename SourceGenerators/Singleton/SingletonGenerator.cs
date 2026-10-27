using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GodotUtilities.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class SingletonGenerator : IIncrementalGenerator
{
    private const string SingletonAttributeFullName = "GodotUtilities.SingletonAttribute";

    private static readonly string[] ReservedMembers =
    [
        "Instance",
        "HasInstance",
        "TryGetInstance",
        "WhenReady",
        "IsSingletonLive",
        "OnSingletonNodeAdded",
        "FindSingletonInstance",
        "FindSingletonInstanceIn",
        "ResolveSingletonInstance",
        "CreateSingletonInstance",
        "_singletonInstance",
        "_singletonReadyCallbacks",
        "_singletonWatchedTree",
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SingletonAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => Transform(ctx, ct))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(models, static (spc, model) => Emit(spc, model));
    }

    private static SingletonModel? Transform(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } type ||
            ctx.TargetNode is not ClassDeclarationSyntax declaration ||
            ctx.Attributes.Length == 0)
        {
            return null;
        }

        var attribute = ctx.Attributes[0];

        var kind = SingletonKind.Scene;
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is int raw and >= 0 and <= 2)
        {
            kind = (SingletonKind)raw;
        }

        string? name = null;
        string? scenePath = null;
        foreach (var argument in attribute.NamedArguments)
        {
            switch (argument.Key)
            {
                case "Name":
                    name = argument.Value.Value as string;
                    break;
                case "ScenePath":
                    scenePath = argument.Value.Value as string;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(name)) name = null;
        if (string.IsNullOrWhiteSpace(scenePath)) scenePath = null;

        ct.ThrowIfCancellationRequested();

        var chain = EnclosingTypes.Build(type, out var allPartial);
        var isNode = GodotSymbols.InheritsFromGodotNode(type);
        var isGodotObject = GodotSymbols.InheritsFromGodotObject(type);
        var location = LocationInfo.From(declaration.Identifier.GetLocation());

        var diagnostics = new List<DiagnosticData>();
        var canGenerate = true;

        void Report(DiagnosticDescriptor descriptor, params string[] args)
        {
            diagnostics.Add(DiagnosticData.Create(descriptor, location, args));
            if (descriptor.DefaultSeverity == DiagnosticSeverity.Error)
                canGenerate = false;
        }

        if (!allPartial)
            Report(SingletonDiagnostics.NotPartial, type.Name);

        if (type.IsAbstract || type.IsStatic || IsOrIsInsideGenericType(type))
            Report(SingletonDiagnostics.UnsupportedType, type.Name);

        if (kind != SingletonKind.Lazy && !isNode)
            Report(SingletonDiagnostics.RequiresNode, type.Name, kind.ToString());

        var createsNode = kind == SingletonKind.Lazy && isNode;

        if (kind == SingletonKind.Lazy && !isNode &&
            !type.InstanceConstructors.Any(static c => c.Parameters.All(static p => p.IsOptional || p.IsParams)))
        {
            Report(SingletonDiagnostics.NoParameterlessConstructor, type.Name);
        }

        if (name is not null && kind == SingletonKind.Scene)
            Report(SingletonDiagnostics.OptionIgnored, type.Name, "Name", "a Scene singleton is named in the scene");
        
        else if (name is not null && kind == SingletonKind.Lazy && !isNode)
            Report(SingletonDiagnostics.OptionIgnored, type.Name, "Name", "only nodes have a name");

        if (scenePath is not null && !createsNode)
            Report(SingletonDiagnostics.OptionIgnored, type.Name, "ScenePath", "it only applies to a Lazy singleton that is a Node");

        foreach (var reserved in ReservedMembers)
        {
            if (type.GetMembers(reserved).Length > 0)
            {
                Report(SingletonDiagnostics.MemberNameCollision, type.Name, reserved);
                break;
            }
        }

        return new SingletonModel(
            Name: type.Name,
            Namespace: type.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : string.Empty,
            EnclosingChain: chain,
            Kind: kind,
            NodeName: name ?? type.Name,
            ScenePath: scenePath,
            IsNode: isNode,
            IsGodotObject: isGodotObject,
            Diagnostics: new EquatableArray<DiagnosticData>(diagnostics),
            CanGenerate: canGenerate);
    }

    private static bool IsOrIsInsideGenericType(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0)
                return true;
        }

        return false;
    }

    private static void Emit(SourceProductionContext spc, SingletonModel model)
    {
        foreach (var diagnostic in model.Diagnostics)
            spc.ReportDiagnostic(diagnostic.ToDiagnostic());

        if (!model.CanGenerate)
            return;

        spc.AddSource(
            EnclosingTypes.GetHintName(model.Namespace, model.EnclosingChain, "Singleton.g.cs"),
            GenerateSource(model));
    }

    private static string GenerateSource(SingletonModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by GodotUtilities.SourceGenerators.SingletonGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        var hasNamespace = model.Namespace.Length != 0;
        if (hasNamespace)
            sb.Append("namespace ").AppendLine(model.Namespace).AppendLine("{");

        var baseIndent = hasNamespace ? "    " : "";
        var bodyIndent = EnclosingTypes.AppendOpen(sb, baseIndent, model.EnclosingChain);

        var body = model.Kind switch
        {
            SingletonKind.Scene => SceneBody(model),
            SingletonKind.Autoload => AutoloadBody(model),
            _ => LazyBody(model),
        };

        AppendIndented(sb, bodyIndent, body);

        EnclosingTypes.AppendClose(sb, baseIndent, model.EnclosingChain);

        if (hasNamespace)
            sb.AppendLine("}");

        return sb.ToString();
    }

    private static void AppendIndented(StringBuilder sb, string indent, string body)
    {
        foreach (var rawLine in body.TrimEnd().Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
            {
                sb.AppendLine();
                continue;
            }

            sb.Append(indent).AppendLine(line);
        }
    }

    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, quote: true);

    private static string SceneBody(SingletonModel model)
    {
        var n = model.Name;
        var noInstance = Literal(
            n + " has no instance. Add it to a scene and access it after that scene has loaded, "
            + "or check HasInstance / TryGetInstance first.");

        return $$"""
            private static {{n}}? _singletonInstance;
            private static global::System.Action<{{n}}>? _singletonReadyCallbacks;
            private static global::Godot.SceneTree? _singletonWatchedTree;

            /// <summary>
            /// The scene's instance, found in the scene tree on first access and cached. Throws if no
            /// <see cref="{{n}}"/> is in the tree; use <see cref="HasInstance"/>, <see cref="TryGetInstance"/>
            /// or <see cref="WhenReady"/> where it may not exist yet.
            /// </summary>
            public static {{n}} Instance
            {
                get
                {
                    if (IsSingletonLive(_singletonInstance))
                        return _singletonInstance!;

                    return FindSingletonInstance()
                        ?? throw new global::System.InvalidOperationException({{noInstance}});
                }
            }

            /// <summary>Whether a <see cref="{{n}}"/> is in the scene tree.</summary>
            public static bool HasInstance => TryGetInstance(out _);

            /// <summary>Gets the instance without throwing.</summary>
            /// <returns><c>true</c> if a <see cref="{{n}}"/> is in the scene tree.</returns>
            public static bool TryGetInstance([global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{n}}? instance)
            {
                instance = IsSingletonLive(_singletonInstance) ? _singletonInstance : FindSingletonInstance();
                return instance != null;
            }

            /// <summary>
            /// Runs <paramref name="callback"/> with the instance: right away if it is already in the scene tree,
            /// otherwise once, shortly after it is added (deferred, so the node is ready by then).
            /// </summary>
            public static void WhenReady(global::System.Action<{{n}}> callback)
            {
                if (TryGetInstance(out var instance))
                {
                    callback(instance);
                    return;
                }

                _singletonReadyCallbacks += callback;

                if (_singletonWatchedTree != null || global::Godot.Engine.GetMainLoop() is not global::Godot.SceneTree tree)
                    return;

                _singletonWatchedTree = tree;
                tree.NodeAdded += OnSingletonNodeAdded;
            }

            private static void OnSingletonNodeAdded(global::Godot.Node node)
            {
                if (node is not {{n}})
                    return;

                _singletonWatchedTree!.NodeAdded -= OnSingletonNodeAdded;
                _singletonWatchedTree = null;

                var callbacks = _singletonReadyCallbacks;
                _singletonReadyCallbacks = null;
                if (callbacks == null)
                    return;

                global::Godot.Callable.From(() =>
                {
                    if (TryGetInstance(out var instance))
                        callbacks(instance);
                }).CallDeferred();
            }

            // A node that was freed, queued for freeing (e.g. the old scene during a scene change) or removed
            // from the tree no longer counts as the instance.
            private static bool IsSingletonLive({{n}}? instance)
                => instance != null
                   && global::Godot.GodotObject.IsInstanceValid(instance)
                   && instance.IsInsideTree()
                   && !instance.IsQueuedForDeletion();

            private static {{n}}? FindSingletonInstance()
                => _singletonInstance = global::Godot.Engine.GetMainLoop() is global::Godot.SceneTree tree
                    ? FindSingletonInstanceIn(tree.Root)
                    : null;

            // Matches by type, so the node's name in the scene doesn't matter. Every node in the tree is a
            // candidate, including ones whose own _EnterTree hasn't run yet.
            private static {{n}}? FindSingletonInstanceIn(global::Godot.Node parent)
            {
                var count = parent.GetChildCount();
                for (var i = 0; i < count; i++)
                {
                    var child = parent.GetChild(i);

                    if (child is {{n}} match && !match.IsQueuedForDeletion())
                        return match;

                    if (FindSingletonInstanceIn(child) is { } nested)
                        return nested;
                }

                return null;
            }
            """;
    }

    private static string AutoloadBody(SingletonModel model)
    {
        var n = model.Name;
        var nameLiteral = Literal(model.NodeName);
        var notFound = Literal(
            "Autoload '" + model.NodeName + "' was not found. Add " + n
            + " in Project Settings > Globals > Autoload with the node name '" + model.NodeName + "'.");

        return $$"""
            private static {{n}}? _singletonInstance;

            /// <summary>
            /// The autoload instance, looked up under the scene tree root as {{model.NodeName}}.
            /// Throws if it isn't registered. Autoloads are added to the tree in order, so a later autoload
            /// can't be reached from an earlier one's <c>_EnterTree</c>; it can from <c>_Ready</c>.
            /// </summary>
            public static {{n}} Instance
            {
                get
                {
                    if (global::Godot.GodotObject.IsInstanceValid(_singletonInstance))
                        return _singletonInstance!;

                    return ResolveSingletonInstance()
                        ?? throw new global::System.InvalidOperationException({{notFound}});
                }
            }

            /// <summary>Whether the autoload is currently in the scene tree.</summary>
            public static bool HasInstance
                => global::Godot.GodotObject.IsInstanceValid(_singletonInstance) || ResolveSingletonInstance() != null;

            /// <summary>Gets the instance without throwing.</summary>
            /// <returns><c>true</c> if the autoload is in the scene tree.</returns>
            public static bool TryGetInstance([global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{n}}? instance)
            {
                if (global::Godot.GodotObject.IsInstanceValid(_singletonInstance))
                {
                    instance = _singletonInstance!;
                    return true;
                }

                instance = ResolveSingletonInstance();
                return instance != null;
            }

            private static {{n}}? ResolveSingletonInstance()
            {
                if (global::Godot.Engine.GetMainLoop() is not global::Godot.SceneTree tree)
                    return null;

                return _singletonInstance = tree.Root.GetNodeOrNull<{{n}}>({{nameLiteral}});
            }
            """;
    }

    private static string LazyBody(SingletonModel model)
    {
        var n = model.Name;

        var isValid = model.IsGodotObject
            ? "global::Godot.GodotObject.IsInstanceValid(_singletonInstance)"
            : "_singletonInstance != null";

        var head = $$"""
            private static {{n}}? _singletonInstance;

            /// <summary>The instance. Created on first access.</summary>
            public static {{n}} Instance
            {
                get
                {
                    if (!({{isValid}}))
                        _singletonInstance = CreateSingletonInstance();

                    return _singletonInstance!;
                }
            }

            /// <summary>Whether the instance has already been created. Never creates it.</summary>
            public static bool HasInstance => {{isValid}};

            /// <summary>Gets the instance only if it has already been created; never creates it.</summary>
            public static bool TryGetInstance([global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{n}}? instance)
            {
                instance = HasInstance ? _singletonInstance : null;
                return instance != null;
            }
            """;

        if (!model.IsNode)
        {
            return head + "\n\n" + $$"""
                private static {{n}} CreateSingletonInstance() => new {{n}}();
                """;
        }

        var nameLiteral = Literal(model.NodeName);
        var noTree = Literal("Can't create " + n + ": the SceneTree isn't available yet.");

        string create;
        if (model.ScenePath is { } scenePath)
        {
            var pathLiteral = Literal(scenePath);
            var cantLoad = Literal("Can't create " + n + ": couldn't load the scene '" + scenePath + "'.");
            var wrongRoot = Literal("Can't create " + n + ": the root node of '" + scenePath + "' isn't a " + n + ".");

            create = $$"""
                var scene = global::Godot.GD.Load<global::Godot.PackedScene>({{pathLiteral}})
                    ?? throw new global::System.InvalidOperationException({{cantLoad}});

                var instance = scene.Instantiate() as {{n}}
                    ?? throw new global::System.InvalidOperationException({{wrongRoot}});
                """;
        }
        else
        {
            create = $$"""
                var instance = new {{n}}();
                """;
        }

        var createMethod = $$"""
            // Lives under the scene tree root, like an autoload. Added deferred, because adding to the root while
            // it is still setting up its children (startup, autoload _Ready) fails.
            private static {{n}} CreateSingletonInstance()
            {
                if (global::Godot.Engine.GetMainLoop() is not global::Godot.SceneTree tree)
                    throw new global::System.InvalidOperationException({{noTree}});

                // Something else (an autoload entry, a scene) may already have put one under the root.
                var existing = tree.Root.GetNodeOrNull<{{n}}>({{nameLiteral}});
                if (existing != null)
                    return existing;

            {{Indent(create, 4)}}
                instance.Name = {{nameLiteral}};

                tree.Root.CallDeferred(global::Godot.Node.MethodName.AddChild, instance);
                return instance;
            }
            """;

        return head + "\n\n" + createMethod;
    }

    private static string Indent(string text, int spaces)
    {
        var pad = new string(' ', spaces);
        return string.Join("\n", text.Split('\n').Select(l => l.TrimEnd('\r')).Select(l => l.Length == 0 ? l : pad + l));
    }
}
