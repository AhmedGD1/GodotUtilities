namespace GodotUtilities.Logic;

/// <summary>
/// A single state of a <see cref="SimpleStateMachine{T}"/>, with optional enter, update, and exit callbacks,
/// a minimum duration, and a re-entry cooldown.
/// </summary>
/// <typeparam name="T">The enum type that identifies states.</typeparam>
/// <param name="id">The id of this state.</param>
public sealed class State<T>(T id) where T : Enum
{
    /// <summary>The id of this state.</summary>
    public T Id { get; } = id;
    
    internal Action<double> Update { get; private set; }
    internal Action Enter { get; private set; }
    internal Action Exit { get; private set; }

    internal double MinDuration { get; private set; }
    internal double Cooldown { get; private set; }

    internal double CapturedElapsed { get; set; } = double.NegativeInfinity;

    /// <summary>Sets the callback run on every update while this state is active. Replaces any previous callback.</summary>
    /// <param name="action">The callback, which receives the delta time in seconds.</param>
    /// <returns>This state, for chaining.</returns>
    public State<T> OnUpdate(Action<double> action)
    {
        Update = action;
        return this;
    }

    /// <summary>Sets the callback run when this state is entered. Replaces any previous callback.</summary>
    /// <param name="action">The callback.</param>
    /// <returns>This state, for chaining.</returns>
    public State<T> OnEnter(Action action)
    {
        Enter = action;
        return this;
    }
    
    /// <summary>Sets the callback run when this state is exited. Replaces any previous callback.</summary>
    /// <param name="action">The callback.</param>
    /// <returns>This state, for chaining.</returns>
    public State<T> OnExit(Action action)
    {
        Exit = action;
        return this;
    }

    /// <summary>
    /// Sets how long this state must be active before the machine can leave it.
    /// Until then, state changes are refused.
    /// </summary>
    /// <param name="duration">The minimum time in seconds. Defaults to 0 (no minimum).</param>
    /// <returns>This state, for chaining.</returns>
    public State<T> SetMinDuration(double duration)
    {
        MinDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets how long after leaving this state it cannot be entered again.
    /// A state that has never been left is always enterable.
    /// </summary>
    /// <param name="duration">The cooldown in seconds. Defaults to 0 (no cooldown).</param>
    /// <returns>This state, for chaining.</returns>
    public State<T> SetCooldown(double duration)
    {
        Cooldown = duration;
        return this;
    }

    internal bool CanExit(double elapsed) => elapsed >= MinDuration;
    internal bool CanEnter(double totalElapsed) => totalElapsed >= (CapturedElapsed + Cooldown);
}
