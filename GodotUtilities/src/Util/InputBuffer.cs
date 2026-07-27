using System.Runtime.InteropServices;
using Godot;

namespace GodotUtilities;

/// <summary>
/// Tracks time-limited "buffered" input actions, allowing an input registered
/// slightly before it becomes usable (e.g. a jump pressed just before landing)
/// to still be consumed within a configurable window.
/// </summary>
public class InputBuffer
{
    private readonly struct BufferedAction
    {
        public StringName Name { get; init; }
        public double RemainingTime { get; init; }

        public BufferedAction Tick(double dt) => this with { RemainingTime = RemainingTime - dt };
        public BufferedAction WithDuration(double duration) => this with { RemainingTime = duration };

        public bool IsValid() => RemainingTime > 0.0;
    }

    private readonly List<BufferedAction> _actions = [];

    /// <summary>
    /// Buffers <paramref name="name"/> for <paramref name="duration"/> seconds. If the
    /// action is already buffered, its remaining time is reset to <paramref name="duration"/>.
    /// </summary>
    /// <param name="name">The action name to buffer.</param>
    /// <param name="duration">How long, in seconds, the action remains valid. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is less than or equal to zero.</exception>
    public void BufferAction(StringName name, double duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, 0.0);

        var span = CollectionsMarshal.AsSpan(_actions);

        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Name == name)
            {
                span[i] = span[i].WithDuration(duration);
                return;
            }
        }

        _actions.Add(new BufferedAction { Name = name, RemainingTime = duration });
    }

    /// <summary>
    /// Attempts to consume a still-valid buffered action named <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The action name to consume.</param>
    /// <returns><c>true</c> if a valid buffered action was found and removed; otherwise <c>false</c>.</returns>
    public bool TryConsume(StringName name)
    {
        var span = CollectionsMarshal.AsSpan(_actions);

        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Name == name && span[i].IsValid())
            {
                RemoveAction(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Advances all buffered actions by <paramref name="dt"/> seconds, removing any
    /// whose remaining time has expired. Should be called once per frame/physics tick.
    /// </summary>
    /// <param name="dt">Elapsed time, in seconds, since the last tick.</param>
    public void Tick(double dt)
    {
        var span = CollectionsMarshal.AsSpan(_actions);

        for (int i = span.Length - 1; i >= 0; i--)
        {
            span[i] = span[i].Tick(dt);

            if (!span[i].IsValid())
            {
                RemoveAction(i);
                span = CollectionsMarshal.AsSpan(_actions);
            }
        }
    }

    /// <summary>
    /// Removes all currently buffered actions, regardless of remaining time.
    /// </summary>
    public void ConsumeAll()
    {
        for (int i = _actions.Count - 1; i >= 0; i--)
            RemoveAction(i);
    }

    /// <summary>
    /// Determines whether <paramref name="name"/> is currently buffered and still valid.
    /// </summary>
    /// <param name="name">The action name to check.</param>
    /// <returns><c>true</c> if a valid buffered action with this name exists; otherwise <c>false</c>.</returns>
    public bool Has(StringName name)
    {
        foreach (var action in CollectionsMarshal.AsSpan(_actions))
            if (action.Name == name && action.IsValid()) return true;
        return false;
    }

    private void RemoveAction(int index)
    {
        int last = _actions.Count - 1;

        if (index != last)
            _actions[index] = _actions[last];

        _actions.RemoveAt(last);
    }
}
