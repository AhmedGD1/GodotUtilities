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

    /// <summary>Subscribes <paramref name="listener"/> to fire exactly once, then auto-removes.</summary>
    public static void AddListenerOnce<T>(Action<T> listener, Node owner = null)
    {
        Action<T> wrapper = null!;
        wrapper = evt => { RemoveListener(wrapper); listener(evt); };
        AddListener(wrapper, owner);
    }

    /// <summary>Unsubscribes a previously registered listener.</summary>
    public static void RemoveListener<T>(Action<T> listener)
    {
        if (_buckets.TryGetValue(typeof(T), out var raw))
            ((TypedBucket<T>)raw).Remove(listener);
    }

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

    internal static void RegisterWired<T>(Action<T> listener, Node owner)
    {
        GetOrCreate<T>().Add(listener);
        owner.TreeExiting += () => RemoveListener(listener);
    }

    private interface IClearable { void Clear(); }

    private sealed class TypedBucket<T> : IClearable
    {
        private Action<T>[] _handlers = [];
        
        private readonly HashSet<Action<T>> _set = [];
        private readonly List<Action<T>> _pending = [];
        
        private int _count;
        private bool _firing;

        public void Add(Action<T> h)
        {
            if (!_set.Add(h))
            {
                GD.PushError($"[EventBus] Duplicate listener for {typeof(T).Name}.");
                return;
            }
            
            if (_count == _handlers.Length)
                Array.Resize(ref _handlers, Math.Max(4, _count * 2));
            _handlers[_count++] = h;
        }

        public void Remove(Action<T> h)
        {
            if (_firing) { _pending.Add(h); return; }
            DoRemove(h);
        }

        private void DoRemove(Action<T> h)
        {
            if (!_set.Remove(h)) return;
            for (int i = 0; i < _count; i++)
            {
                if (_handlers[i] != h) continue;

                Array.Copy(_handlers, i + 1, _handlers, i, _count - i - 1);
                _handlers[--_count] = null!;
                return;
            }
        }

        public void Fire(T evt)
        {
            _firing = true;
            try { for (int i = 0; i < _count; i++) _handlers[i]?.Invoke(evt); }
            finally
            {
                _firing = false;
                if (_pending.Count > 0) { foreach (var h in _pending) DoRemove(h); _pending.Clear(); }
            }
        }

        public void Clear()
        {
            Array.Clear(_handlers, 0, _count);
            _count = 0;
            
            _set.Clear(); 
            _pending.Clear();
        }
    }

    private static TypedBucket<T> GetOrCreate<T>()
    {
        if (!_buckets.TryGetValue(typeof(T), out var raw))
            _buckets[typeof(T)] = raw = new TypedBucket<T>();
        return (TypedBucket<T>)raw;
    }
}
