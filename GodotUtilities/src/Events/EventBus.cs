using Godot;

namespace GodotUtilities.Events;

/// <summary>
/// A lightweight, type-safe event bus for Godot C#.
///
/// THREADING CONTRACT: All methods must be called from the Godot main thread.
/// The bus performs no locking. Calling from a background thread causes silent data corruption.
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, object> _buckets = [];
    private static readonly HashSet<Node> _wired = [];

    private static TypedBucket<T> GetOrCreate<T>()
    {
        if (!_buckets.TryGetValue(typeof(T), out var raw))
            _buckets[typeof(T)] = raw = new TypedBucket<T>();
            
        return (TypedBucket<T>)raw;
    }

    #region Register
    
    /// <summary>
    /// Subscribes <paramref name="listener"/> to events of type <typeparamref name="T"/>.
    /// Pass <paramref name="owner"/> to auto-remove when the node leaves the scene tree.
    /// </summary>
    public static void AddListener<T>(Action<T> listener, Node owner = null)
    {
        GetOrCreate<T>().Add(listener);
        if (owner != null)
            owner.TreeExiting += () => RemoveListener(listener);
    }

    /// <summary>Unsubscribes a previously registered listener.</summary>
    public static bool RemoveListener<T>(Action<T> listener)
    {
        if (_buckets.TryGetValue(typeof(T), out var raw) && ((TypedBucket<T>)raw).Remove(listener))
            return true;
        return false;
    }
    
    #endregion

    #region Trigger
    
    /// <summary>Fires all listeners registered for <typeparamref name="T"/>.</summary>
    public static void Trigger<T>(T evt)
    {
        if (evt is null)
        {
            GD.PushError($"[EventBus] Null event passed to Trigger<{typeof(T).Name}>.");
            return;
        }
        if (_buckets.TryGetValue(typeof(T), out var raw))
            ((TypedBucket<T>)raw).Fire(evt);
    }

    /// <summary>Fires using a default instance. <typeparamref name="T"/> needs a parameterless constructor.</summary>
    public static void Trigger<T>() where T : new() => Trigger(new T());

    #endregion

    #region Queries
    
    /// <summary>Clears all listeners across every event type.</summary>
    public static void Clear()
    {
        foreach (var b in _buckets.Values) ((IClearable)b).Clear();
        _buckets.Clear();
    }

    /// <summary>Clears all listeners for <typeparamref name="T"/> only.</summary>
    public static void Clear<T>()
    {
        if (_buckets.TryGetValue(typeof(T), out var raw))
            ((TypedBucket<T>)raw).Clear();
    }

    #endregion

    #region For Source Generator

    public static bool TryBeginWiring(Node node)
    {
        if (!_wired.Add(node))
        {
            GD.PushError($"[EventBus] WireEvents called twice on '{node.Name}' ({node.GetType().Name}). Ignoring duplicate.");
            return false;
        }

        node.TreeExiting += () => _wired.Remove(node);
        return true;
    }
    
    #endregion
}
