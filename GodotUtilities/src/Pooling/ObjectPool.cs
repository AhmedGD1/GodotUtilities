using Godot;

namespace GodotUtilities.Pooling;

/// <summary>
/// A generic object pool that recycles instances of <typeparamref name="T"/> to avoid
/// repeated allocation. Instances implementing <see cref="IPoolable"/> are notified via
/// <see cref="IPoolable.OnGet"/>/<see cref="IPoolable.OnRelease"/> in addition to the
/// optional <c>onGet</c>/<c>onRelease</c> callbacks.
/// </summary>
/// <typeparam name="T">The pooled type.</typeparam>
/// <param name="factory">Creates a new instance of <typeparamref name="T"/> when the pool needs one.</param>
/// <param name="extensible">
/// If <c>true</c> (default), the pool creates new instances via <paramref name="factory"/>
/// when empty rather than failing. See <see cref="Extensible"/>.
/// </param>
/// <param name="onGet">Optional callback invoked with the instance whenever one is retrieved.</param>
/// <param name="onRelease">Optional callback invoked with the instance whenever one is returned.</param>
public class ObjectPool<T>(Func<T> factory, bool extensible = true, Action<T> onGet = null, Action<T> onRelease = null)
{
    private readonly HashSet<T> active = [];
    private readonly Stack<T> free = new();

    /// <summary>
    /// If <c>true</c>, <see cref="TryGet"/> creates a new instance via the pool's factory
    /// when no free instance is available, instead of failing.
    /// </summary>
    public bool Extensible { get; set; } = extensible;

    /// <summary>The number of instances currently checked out from the pool.</summary>
    public int ActiveCount => active.Count;

    /// <summary>The number of instances currently available for reuse.</summary>
    public int FreeCount => free.Count;

    /// <summary>The total number of instances tracked by the pool (active + free).</summary>
    public int TotalCount => ActiveCount + FreeCount;

    /// <summary>
    /// Eagerly creates <paramref name="count"/> instances via the factory and adds them
    /// to the free pool, so later <see cref="TryGet"/>/<see cref="Get"/> calls avoid
    /// allocating.
    /// </summary>
    /// <param name="count">The number of instances to pre-create.</param>
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
            free.Push(factory());
    }

    /// <summary>
    /// Attempts to retrieve an instance from the pool: a free instance if one is available,
    /// otherwise a newly-created one if <see cref="Extensible"/> is <c>true</c>. Any freed
    /// <see cref="GodotObject"/> instances found invalid at the top of the free stack are
    /// discarded first. On success, <see cref="IPoolable.OnGet"/> and the <c>onGet</c>
    /// callback (if provided) are invoked.
    /// </summary>
    /// <param name="obj">The retrieved instance, or <c>default</c> if the pool is exhausted and not extendable.</param>
    /// <returns><c>true</c> if an instance was retrieved; otherwise <c>false</c>.</returns>
    public bool TryGet(out T obj)
    {
        while (free.TryPeek(out var raw) && raw is GodotObject godotObject && !GodotObject.IsInstanceValid(godotObject))
            free.Pop();

        if (free.TryPop(out var result)) obj = result;
        else if (Extensible) obj = factory();
        else
        {
            obj = default;
            return false;
        }

        active.Add(obj);

        if (obj is IPoolable poolable)
            poolable.OnGet();
        onGet?.Invoke(obj);
        
        return true;
    }
    
    /// <summary>
    /// Retrieves an instance from the pool. Unlike <see cref="TryGet"/>, exhaustion is
    /// reported via a pushed warning rather than a return value.
    /// </summary>
    /// <returns>The retrieved instance, or <c>default</c> if the pool is exhausted and not extendable.</returns>
    public T Get()
    {
        if (!TryGet(out T obj))
        {
            GD.PushWarning($"Pool for {typeof(T).Name} exhausted!");
            return default;
        }
        
        return obj;
    }

    /// <summary>
    /// Attempts to return <paramref name="obj"/> to the pool for reuse. On success,
    /// <see cref="IPoolable.OnRelease"/> and the <c>onRelease</c> callback (if provided)
    /// are invoked.
    /// </summary>
    /// <param name="obj">The instance to release.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="obj"/> was active in this pool and has been released;
    /// <c>false</c> if it was not tracked as active (e.g. already released, or not from this pool).
    /// </returns>
    public bool TryRelease(T obj)
    {
        if (!active.Contains(obj))
            return false;

        active.Remove(obj);
        free.Push(obj);

        if (obj is IPoolable poolable)
            poolable.OnRelease();
        onRelease?.Invoke(obj);

        return true;
    }

    /// <summary>
    /// Returns <paramref name="obj"/> to the pool. Unlike <see cref="TryRelease"/>, failure
    /// (the instance wasn't active) is reported via a pushed warning rather than a return value.
    /// </summary>
    /// <param name="obj">The instance to release.</param>
    public void Release(T obj)
    {
        if (!TryRelease(obj))
            GD.PushWarning($"{typeof(T).Name} returned to the wrong pool or already released.");
    }

    /// <summary>
    /// Releases every currently active instance back to the free pool, invoking
    /// <see cref="IPoolable.OnRelease"/> and the <c>onRelease</c> callback for each.
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var obj in active)
        {
            if (obj is IPoolable poolable)
                poolable.OnRelease();
            onRelease?.Invoke(obj);
            free.Push(obj);
        }

        active.Clear();
    }

    /// <summary>
    /// Discards free (non-active) instances down to <paramref name="targetSize"/>, most
    /// recently freed first. Does not affect active instances.
    /// </summary>
    /// <param name="targetSize">The maximum number of free instances to retain.</param>
    /// <param name="onTrim">Optional callback invoked with each discarded instance.</param>
    public void Trim(int targetSize, Action<T> onTrim = null)
    {
        while (free.Count > targetSize)
        {
            var obj = free.Pop();
            onTrim?.Invoke(obj);
        }
    }

    /// <summary>
    /// Releases all active instances and then discards the entire free pool, leaving the
    /// pool empty.
    /// </summary>
    public void Clear()
    {
        ReleaseAll();
        free.Clear();
    }
}
