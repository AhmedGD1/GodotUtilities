using System.Collections.Generic;
using Godot;

namespace Utilities.Pooling;

public interface IPoolable
{
    void OnGet();
    void OnRelease();
}

public class NodePool<T> where T : Node
{
    private readonly PackedScene prefab;
    private readonly Node parent;
    private readonly Stack<T> available = new();
    private readonly HashSet<T> inUse   = new();

    private readonly bool extendable;

    public int TotalCount  => available.Count + inUse.Count;
    public int ActiveCount => inUse.Count;
    public int FreeCount   => available.Count;

    public IEnumerable<T> InUse => inUse;

    public NodePool(PackedScene prefab, Node parent, int initialSize = 0, bool extendable = true)
    {
        this.prefab     = prefab;
        this.parent     = parent;
        this.extendable = extendable;

        Prewarm(initialSize);
    }

    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
            available.Push(CreateNew());
    }

    private T CreateNew()
    {
        var instance = prefab.Instantiate<T>();
        SetVisible(instance, false);
        parent.AddChildDeferred(instance);
        return instance;
    }

    public T Get()
    {
        T node;

        if (available.Count > 0) node = available.Pop();
        else if (extendable)     node = CreateNew();
        else
        {
            GD.PushWarning($"Pool for {typeof(T).Name} exhausted!");
            return null;
        }

        SetVisible(node, true);
        inUse.Add(node);

        if (node is IPoolable poolable)
            poolable.OnGet();
        return node;
    }

    public void Release(T node)
    {
        if (!inUse.Contains(node))
        {
            GD.PushWarning($"{typeof(T).Name} returned to the wrong pool or already released.");
            return;
        }
        
        inUse.Remove(node);
        available.Push(node);

        SetVisible(node, false);

        if (node is IPoolable poolable)
            poolable.OnRelease();
    }

    public bool TryRelease(T node)
    {
        if (!inUse.Contains(node)) 
            return false;
        Release(node);
        return true;
    }

    public void ReleaseAll()
    {
        foreach (var node in inUse)
        {
            SetVisible(node, false);
            if (node is IPoolable poolable)
                poolable.OnRelease();
            available.Push(node);
        }
        inUse.Clear();
    }

    public void Destroy()
    {
        foreach (var node in inUse)   node.QueueFree();
        foreach (var node in available) node.QueueFree();

        available.Clear();
        inUse.Clear();
    }

    private static void SetVisible(Node node, bool value)
    {
        switch (node)
        {
            case CanvasItem ci:
                ci.Visible = value;
                break;
            
            case Node3D n3d:
                n3d.Visible = value;
                break;
        }
    }



}

