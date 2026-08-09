namespace GodotUtilities.Events;

/// <summary>
/// Marks a method as an event handler to be wired up by the generated
/// WireEvents() method. The event type is inferred from the method's single
/// parameter, or can be given explicitly for parameterless handlers.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class EventHandlerAttribute : Attribute
{
    public Type EventType { get; }

    public EventHandlerAttribute() { }

    public EventHandlerAttribute(Type eventType) => EventType = eventType;
}
