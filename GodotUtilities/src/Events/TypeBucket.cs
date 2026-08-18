using Godot;

namespace GodotUtilities.Events;

internal interface IClearable
{
    void Clear(); 
}

internal sealed class TypedBucket<T> : IClearable
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

    public bool Remove(Action<T> h)
    {
        if (_firing)
        {
            if (!_set.Contains(h))
                return false;
                
            _pending.Add(h);
            return true;
        }
        return DoRemove(h);
    }

    private bool DoRemove(Action<T> h)
    {
        if (!_set.Remove(h)) 
            return false;
        
        for (int i = 0; i < _count; i++)
        {
            if (_handlers[i] != h) continue;

            Array.Copy(_handlers, i + 1, _handlers, i, _count - i - 1);
            _handlers[--_count] = null!;
            return true;
        }
        return true;
    }

    public void Fire(T evt)
    {
        _firing = true;
        try
        {
            for (int i = 0; i < _count; i++) 
                _handlers[i]?.Invoke(evt);
        }
        finally
        {
            _firing = false;
            
            if (_pending.Count > 0)
            {
                foreach (var h in _pending)
                    DoRemove(h); 
                _pending.Clear();
            }
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
