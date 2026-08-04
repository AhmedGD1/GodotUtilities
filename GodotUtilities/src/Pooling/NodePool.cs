using Godot;

namespace GodotUtilities.Pooling;

/// <summary>
/// A pool of <typeparamref name="T"/> scene instances parented under a shared root node.
/// Unlike <see cref="ObjectPool{T}"/>, this manages Godot node lifecycle directly:
/// instantiating from a <see cref="PackedScene"/>, toggling process mode and visibility
/// on get/release, and freeing nodes (deferred) on <see cref="Clear"/>/<see cref="Trim"/>.
/// </summary>
/// <typeparam name="T">The pooled node type.</typeparam>
/// <param name="packedScene">The scene to instantiate for new pool members.</param>
/// <param name="root">The node new instances are parented under.</param>
public class NodePool<T>(PackedScene packedScene, Node root) where T : Node
{
    private readonly HashSet<T> active = [];
    private readonly Stack<T> free = new();

    /// <summary>
    /// If <c>true</c>, <see cref="TryGet"/> instantiates a new node via
    /// <see cref="CreateNew"/> when no free node is available, instead of failing.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool Extendible { get; set; } = true;

    /// <summary>The number of nodes currently checked out from the pool.</summary>
    public int ActiveCount => active.Count;

    /// <summary>The number of nodes currently available for reuse.</summary>
    public int FreeCount => free.Count;

    /// <summary>The total number of nodes tracked by the pool (active + free).</summary>
    public int TotalCount => ActiveCount + FreeCount;

    /// <summary>
    /// Eagerly instantiates <paramref name="count"/> nodes via <see cref="CreateNew"/> and
    /// adds them to the free pool, so later <see cref="TryGet"/>/<see cref="Get"/> calls
    /// avoid instantiating.
    /// </summary>
    /// <param name="count">The number of nodes to pre-create.</param>
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var node = CreateNew();
            free.Push(node);
        }
    }

    /// <summary>
    /// Instantiates a new <typeparamref name="T"/> from the pool's scene, parents it under
    /// the pool's root, and marks it inactive (disabled process mode, hidden).
    /// </summary>
    /// <returns>The newly created, inactive node.</returns>
    /// <exception cref="NullReferenceException">The pool's root node has been freed.</exception>
    public T CreateNew()
    {
        if (!GodotObject.IsInstanceValid(root))
            throw new NullReferenceException("Root node is freed and can't be used.");

        var instance = packedScene.Instantiate<T>();
        root.AddChild(instance);

        SetActive(instance, false);
        return instance;
    }

    /// <summary>
    /// Attempts to retrieve a node from the pool: a free node if one is available,
    /// otherwise a newly-instantiated one if <see cref="Extendible"/> is <c>true</c>.
    /// Free nodes found invalid (freed elsewhere) are discarded first. On success, the
    /// node is marked active (enabled process mode, visible) and
    /// <see cref="IPoolable.OnGet"/> is invoked if implemented.
    /// </summary>
    /// <param name="node">The retrieved node, or <c>null</c> if the pool is exhausted and not extendable.</param>
    /// <returns><c>true</c> if a node was retrieved; otherwise <c>false</c>.</returns>
    public bool TryGet(out T node)
    {
        while (free.TryPeek(out var raw) && raw is GodotObject godotObject && !GodotObject.IsInstanceValid(godotObject))
            free.Pop();

        if (free.TryPop(out var result)) node = result;
        else if (Extendible) node = CreateNew();
        else
        {
            node = null;
            return false;
        }

        SetActive(node, true);
        active.Add(node);

        if (node is IPoolable poolable)
            poolable.OnGet();
        return true;
    }

    /// <summary>
    /// Retrieves a node from the pool. Unlike <see cref="TryGet"/>, exhaustion is
    /// reported via a pushed warning rather than a return value.
    /// </summary>
    /// <returns>The retrieved node, or <c>null</c> if the pool is exhausted and not extendable.</returns>
    public T Get()
    {
        if (!TryGet(out T value))
        {
            GD.PushWarning($"Pool for {typeof(T).Name} exhausted!");
            return null;
        }
        
        return value;
    }

    /// <summary>
    /// Attempts to return <paramref name="node"/> to the pool for reuse: marks it inactive
    /// (disabled process mode, hidden) and invokes <see cref="IPoolable.OnRelease"/> if implemented.
    /// </summary>
    /// <param name="node">The node to release.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="node"/> was active in this pool and has been released;
    /// <c>false</c> if it was not tracked as active (e.g. already released, or not from this pool).
    /// </returns>
    public bool TryRelease(T node)
    {
        if (!active.Contains(node))
            return false;

        SetActive(node, false);

        active.Remove(node);
        free.Push(node);

        if (node is IPoolable poolable)
            poolable.OnRelease();

        return true;
    }

    /// <summary>
    /// Returns <paramref name="node"/> to the pool. Unlike <see cref="TryRelease"/>, failure
    /// (the node wasn't active) is reported via a pushed warning rather than a return value.
    /// </summary>
    /// <param name="node">The node to release.</param>
    public void Release(T node)
    {
        if (!TryRelease(node))
            GD.PushWarning($"{typeof(T).Name} returned to the wrong pool or already released.");
    }

    /// <summary>
    /// Releases every currently active node back to the free pool (marking each inactive
    /// and invoking <see cref="IPoolable.OnRelease"/> if implemented). Nodes found invalid
    /// (freed elsewhere) are skipped rather than re-added to the free pool.
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var node in active)
        {
            if (!GodotObject.IsInstanceValid(node))
                continue;
                
            SetActive(node, false);

            if (node is IPoolable poolable)
                poolable.OnRelease();
            free.Push(node);
        }

        active.Clear();
    }

    /// <summary>
    /// Invokes <see cref="IPoolable.OnRelease"/> where applicable and deferred-frees every
    /// node tracked by the pool (both active and free), then empties the pool. Safe to call
    /// even if some nodes were already freed externally.
    /// </summary>
    public void Clear()
    {
        foreach (var node in active)
        {
            if (GodotObject.IsInstanceValid(node))
            {
                if (node is IPoolable poolable)
                    poolable.OnRelease();
                node.CallDeferred(Node.MethodName.QueueFree);
            }
        }

        foreach (var node in free)
            if (GodotObject.IsInstanceValid(node))
                node.CallDeferred(Node.MethodName.QueueFree);

        free.Clear();
        active.Clear();
    }

    /// <summary>
    /// Deferred-frees free (non-active) nodes down to <paramref name="targetSize"/>, invoking
    /// <see cref="IPoolable.OnRelease"/> and the optional <paramref name="onTrim"/> callback
    /// for each. Does not affect active nodes.
    /// </summary>
    /// <param name="targetSize">The maximum number of free nodes to retain.</param>
    /// <param name="onTrim">Optional callback invoked with each discarded node before it's freed.</param>
    public void Trim(int targetSize, Action<T> onTrim = null)
    {
        while (free.Count > targetSize)
        {
            var node = free.Pop();

            if (!GodotObject.IsInstanceValid(node))
                continue;

            if (node is IPoolable poolable)
                poolable.OnRelease();

            onTrim?.Invoke(node);
            node.CallDeferred(Node.MethodName.QueueFree);
        }
    }

    private static void SetActive(T node, bool value)
    {
        node.ProcessMode = value
            ? Node.ProcessModeEnum.Inherit
            : Node.ProcessModeEnum.Disabled;

        switch (node)
        {
            case CanvasItem ci: ci.Visible = value; break;
            case Node3D n3d: n3d.Visible = value; break;
        }
    }
}
