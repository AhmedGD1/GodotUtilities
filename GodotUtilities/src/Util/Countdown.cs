namespace GodotUtilities;

/// <summary>
/// Represents a lightweight, manually-updated countdown timer.
/// </summary>
/// <remarks>
/// The timer does not update automatically. Call <see cref="Tick(double)"/>
/// during an update loop to advance the countdown.
///
/// <para>
/// <see cref="Tick(double)"/> returns <see langword="true"/> only when the
/// timer reaches zero during that tick, making it useful for detecting
/// one-shot expiration without relying on signals or callbacks.
/// </para>
///
/// <para>
/// A newly created timer is stopped until <see cref="Start()"/> or
/// <see cref="Start(double)"/> is called.
/// </para>
/// </remarks>
/// <param name="duration">
/// The default duration, in seconds, used when calling <see cref="Start()"/>.
/// </param>
public struct Countdown(double duration)
{
    private double currentDuration;
    private double remaining;

    /// <summary>
    /// Gets whether the countdown has finished.
    /// </summary>
    public readonly bool IsFinished => remaining <= 0.0;

    /// <summary>
    /// Gets the amount of time remaining, in seconds.
    /// Returns zero when the countdown has finished.
    /// </summary>
    public readonly double TimeLeft => Math.Max(0.0, remaining);

    /// <summary>
    /// Gets the normalized progress of the countdown from <c>0</c> to <c>1</c>.
    /// </summary>
    /// <remarks>
    /// Returns <c>0</c> when the countdown has just started and <c>1</c>
    /// when it has finished. A zero-duration countdown reports a progress
    /// of <c>1</c>.
    /// </remarks>
    public readonly float Progress => currentDuration != 0.0
        ? (float)(1.0 - (remaining / currentDuration))
        : 1f;

    /// <summary>
    /// Stops the countdown and resets its remaining time to zero.
    /// </summary>
    public void Stop() => remaining = 0.0;

    /// <summary>
    /// Starts or restarts the countdown using its configured default duration.
    /// </summary>
    public void Start() => remaining = currentDuration = duration;

    /// <summary>
    /// Starts or restarts the countdown using the specified duration.
    /// </summary>
    /// <param name="sec">The duration, in seconds.</param>
    public void Start(double sec) => remaining = currentDuration = sec;

    /// <summary>
    /// Advances the countdown by the specified amount of time.
    /// </summary>
    /// <param name="dt">
    /// The elapsed time, in seconds. Negative values are treated as zero.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the countdown reached zero during this tick;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Once the countdown has finished, subsequent calls return
    /// <see langword="false"/> until the countdown is started again.
    /// </remarks>
    public bool Tick(double dt)
    {
        if (IsFinished)
            return false;

        remaining -= Math.Max(0.0, dt);

        if (remaining <= 0.0)
        {
            remaining = 0.0;
            return true;
        }

        return false;
    }
}
