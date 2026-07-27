using System.Reflection;
using Godot;
using LinqExpr = System.Linq.Expressions.Expression;

namespace GodotUtilities.Events;

public static class EventSubscriber
{
    private static readonly Dictionary<Type, WireEntry[]> _cache = [];
    private static readonly HashSet<Node> _wired = [];

    private static readonly BindingFlags _flags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void WireEvents(this Node node)
    {
        if (!_wired.Add(node))
        {
            GD.PushError($"[EventBus] WireEvents called twice on '{node.Name}' ({node.GetType().Name}). Ignoring duplicate.");
            return;
        }

        node.TreeExiting += () => _wired.Remove(node);

        foreach (ref readonly var entry in GetOrBuild(node.GetType()).AsSpan())
            entry.Register(node);
    }

    private static WireEntry[] GetOrBuild(Type nodeType)
    {
        if (_cache.TryGetValue(nodeType, out var cached)) return cached;

        var entries = new List<WireEntry>();

        foreach (var method in nodeType.GetMethods(_flags))
        {
            var attr = method.GetCustomAttribute<EventHandlerAttribute>();
            if (attr is null) continue;

            var parameters = method.GetParameters();
            var eventType = attr.EventType ?? (parameters.Length > 0 ? parameters[0].ParameterType : null);

            if (eventType is null)
            {
                GD.PushError(
                    $"[EventBus] '{nodeType.Name}.{method.Name}': no parameter and no explicit type. " +
                    $"Add a parameter or use [EventHandler(typeof(YourEvent))].");
                continue;
            }

            if (parameters.Length > 0 && parameters[0].ParameterType != eventType)
            {
                GD.PushError(
                    $"[EventBus] '{nodeType.Name}.{method.Name}': " +
                    $"explicit type ({eventType.Name}) != parameter type ({parameters[0].ParameterType.Name}).");
                continue;
            }

            var registerMethod = attr.Once
                ? GetOnceMethod(eventType)
                : GetRegisterWiredMethod(eventType);

            entries.Add(new WireEntry(Compile(method, nodeType, eventType, parameters.Length > 0, registerMethod)));
        }

        return _cache[nodeType] = [.. entries];
    }

    private static Action<Node> Compile(
        MethodInfo method, Type nodeType, Type eventType, bool passEvent, MethodInfo registerMethod)
    {
        var nodeParam = LinqExpr.Parameter(typeof(Node), "node");
        var castNode = LinqExpr.Convert(nodeParam, nodeType);
        var evtParam = LinqExpr.Parameter(eventType, "evt");

        var handlerBody = passEvent
            ? LinqExpr.Call(castNode, method, evtParam)
            : LinqExpr.Call(castNode, method);

        var handler = LinqExpr.Lambda(typeof(Action<>).MakeGenericType(eventType), handlerBody, evtParam);
        var call = LinqExpr.Call(null, registerMethod, handler, nodeParam);

        return LinqExpr.Lambda<Action<Node>>(call, nodeParam).Compile();
    }

    private static MethodInfo GetRegisterWiredMethod(Type eventType) =>
        typeof(EventBus)
            .GetMethod(nameof(EventBus.RegisterWired), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(eventType);

    private static MethodInfo GetOnceMethod(Type eventType) =>
        typeof(EventBus)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(EventBus.AddListenerOnce) &&
                        m.GetParameters() is { Length: 2 } p &&
                        p[0].ParameterType.IsGenericType &&
                        p[0].ParameterType.GetGenericTypeDefinition() == typeof(Action<>))
            .MakeGenericMethod(eventType);
}

internal readonly struct WireEntry(Action<Node> register)
{
    private readonly Action<Node> _register = register;
    public void Register(Node node) => _register(node);
}
