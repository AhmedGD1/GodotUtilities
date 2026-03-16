using System.Collections.Generic;
using System;
using Godot;

namespace Utilities.Pooling;

public class ObjectPool<T>
{
    private readonly Func<T> factory;
    private readonly Stack<T> available = new();
    private readonly HashSet<T> inUse = new();

    private readonly bool extendable;

    public int TotalCount  => available.Count + inUse.Count;
    public int ActiveCount => inUse.Count;
    public int FreeCount   => available.Count;

    public ObjectPool(Func<T> factory, int initialSize = 0, bool extendable = true)
    {
        this.extendable = extendable;
        this.factory    = factory;
        Prewarm(initialSize);
    }

    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
            available.Push(factory());
    }

    public T Get()
    {
        T @object;

        if (available.Count > 0) @object = available.Pop();
        else if (extendable)     @object = factory();
        else
        {
            GD.PushWarning($"Pool for {typeof(T).Name} exhausted!");
            return default;
        }

        inUse.Add(@object);

        if (@object is IPoolable poolable)
            poolable.OnGet();
        return @object;
    }

    public void Release(T @object)
    {
        if (!inUse.Contains(@object))
        {
            GD.PushWarning($"{typeof(T).Name} returned to the wrong pool or already released.");
            return;
        }
        available.Push(@object);
        inUse.Remove(@object);

        if (@object is IPoolable poolable)
            poolable.OnRelease();
    }

    public bool TryRelease(T obj)
    {
        if (!inUse.Contains(obj)) 
            return false;
        Release(obj);
        return true;
    }

    public void ReleaseAll()
    {
        foreach (var obj in inUse)
        {
            if (obj is IPoolable poolable)
                poolable.OnRelease();
            available.Push(obj);
        }
        inUse.Clear();
    }

    public void Clear()
    {
        available.Clear();
        inUse.Clear();
    }
}

