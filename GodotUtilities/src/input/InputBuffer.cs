using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;

namespace Utilities.Inputs;

public class InputBuffer
{
    public event Action<string> Consumed;
    public event Action<string> Expired;

    private readonly List<BufferedAction> actions = new();

    public void BufferAction(string name, float duration)
    {
        var span = CollectionsMarshal.AsSpan(actions);

        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Name == name)
            {
                span[i] = span[i].WithDuration(duration);
                return;
            }
        }

        actions.Add(new BufferedAction { Name = name, ExpireTime = duration });
    }

    public bool TryConsume(string name)
    {
        var span = CollectionsMarshal.AsSpan(actions);

        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Name == name && span[i].IsValid())
            {
                Consumed?.Invoke(name);
                RemoveAtSwap(i);
                return true;
            }
        }

        return false;
    }

    public void Update(float dt)
    {
        // span for fastest iteration
        var span = CollectionsMarshal.AsSpan(actions);

        for (int i = span.Length - 1; i >= 0; i--)
        {
            span[i] = span[i].Tick(dt);

            if (!span[i].IsValid())
            {
                Expired?.Invoke(span[i].Name);

                RemoveAtSwap(i);

                // refresh
                span = CollectionsMarshal.AsSpan(actions);
            }
        }
    }

    private void RemoveAtSwap(int index)
    {
        int last = actions.Count - 1;

        if (index != last)
            actions[index] = actions[last];

        actions.RemoveAt(last);
    }

    public IReadOnlyList<string> GetActions()
    {
        return actions.ConvertAll(a => a.Name);
    }
}

public readonly struct BufferedAction
{
    public string Name      { get; init; }
    public float ExpireTime { get; init; }

    public BufferedAction Tick(float dt)               => this with { ExpireTime = ExpireTime - dt };
    public BufferedAction WithDuration(float duration) => this with { ExpireTime = duration };

    public bool IsValid() => ExpireTime > 0f;
}

