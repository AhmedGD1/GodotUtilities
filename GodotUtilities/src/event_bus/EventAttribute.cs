using System;

namespace Utilities.Events;

[AttributeUsage(AttributeTargets.Method)]
public class EventHandlerAttribute : Attribute
{
    public Type EventType { get; }
    public bool Once      { get; init; }

    public EventHandlerAttribute() { }

    public EventHandlerAttribute(Type eventType)
    {
        EventType = eventType;
    }
}