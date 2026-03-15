using System.Collections.Generic;
using System;

namespace Utilities.InputBuffering;

public class InputBuffer
{
    public event Action<string> Consumed;

    public int ActiveCount => actions.Count;

    private readonly List<BufferedAction> actions = new();

    public void BufferAction(string actionName, float duration)
    {
        BufferedAction existing = actions.Find(b => b.Name == actionName);

        if (existing != null)
        {
            existing.SetDuration(duration);
            return;
        }

        var action = BufferedAction.Get(actionName, duration);
        actions.Add(action);
    }

    public bool HasAction(string action)
    {
        return actions.FindIndex(b => b.IsValid() && b.Name == action) != -1;
    }

    public bool TryConsume(string actionName)
    {
        BufferedAction action = actions.Find(b => b.IsValid() && b.Name == actionName);

        if (action == null) return false;

        ReleaseAction(action);
        actions.Remove(action); 

        return true;
    }

    public void Update(float dt)
    {
        for (int i = actions.Count - 1; i >= 0; i--)
        {
            actions[i].Update(dt);

            if (!actions[i].IsValid())
            {
                ReleaseAction(actions[i]);
                actions[i] = actions[^1];
                actions.RemoveAt(i);
                i--;
            }
        }
    }

    public void Clear()
    {
        for (int i = actions.Count - 1; i >= 0; i--)
            ReleaseAction(actions[i]);
        
        actions.Clear();
    }

    public IReadOnlyList<string> GetActions()
    {
        return actions.ConvertAll(a => a.Name);
    }

    private void ReleaseAction(BufferedAction action)
    {
        Consumed?.Invoke(action.Name);
        BufferedAction.Release(action);
    }
}

public class BufferedAction
{
    public string Name      { get; private set; }
    public float ExpireTime { get; private set; }

    public void SetDuration(float duration) => ExpireTime = duration;
    public void Update(float delta)         => ExpireTime -= delta;
    public bool IsValid()                   => ExpireTime > 0f;

    private readonly static Stack<BufferedAction> pool = new();
    public static void Release(BufferedAction action) => pool.Push(action);

    public static BufferedAction Get(string name, float duration)
    {
        var action = pool.Count > 0 ? pool.Pop() : new BufferedAction();
        action.Name = name;
        action.ExpireTime = duration;
        return action;
    }
}

