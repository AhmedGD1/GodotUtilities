using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace Utilities.Events;

public static class EventSubscriber
{
    private static readonly HashSet<Node> wiredNodes = new();

    public static void WireEvents(this Node node)
    {
        if (!wiredNodes.Add(node))
        {
            GD.PushWarning($"[EventBus] WireEvents called more than once on {node.Name}. Second call ignored.");
            return;
        }

        node.TreeExiting += () => wiredNodes.Remove(node);

        var binding = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var methods = node.GetType().GetMethods(binding);

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<EventHandlerAttribute>();

            if (attribute is null) 
                continue;
            
            var parameters = method.GetParameters();

            Type eventType = attribute.EventType ?? GetEventTypeOrNull(parameters);                                  

            if (eventType == null)
            {
                GD.PushError($"[EventBus] {method.Name} must have a parameter or use [EventHandler(typeof(...))].");
                continue;
            }

            bool passEventToMethod = parameters.Length > 0;

            var args = new object[1];

            Action<object> invoke = passEventToMethod
                ? evt => { args[0] = evt; method.Invoke(node, args); }
                : _   => method.Invoke(node, null);
            
            Action<object> handler = attribute.Once ? MakeOnce(eventType, invoke) : invoke;

            EventBus.Internal.AddListener(eventType, handler, node);
        }
    }

    private static Type GetEventTypeOrNull(ParameterInfo[] parameters) =>
        parameters.Length > 0 ? parameters[0].ParameterType : null;

    private static Action<object> MakeOnce(Type eventType, Action<object> invoke)
    {
        Action<object> handler = null!;

        handler = evt =>
        {
            EventBus.Internal.RemoveListener(eventType, handler);
            invoke(evt);  
        };

        return handler;
    }
}


