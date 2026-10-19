using Godot;

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
/// <param name="baseDuration">
/// The default duration, in seconds, used when calling <see cref="Start()"/>.
/// </param>
public struct Countdown(double baseDuration)
{
    private double _duration, _remaining;

    /// <summary>Whether the countdown is stopped or has reached zero.</summary>
    public readonly bool IsFinished => _remaining <= 0.0;

    /// <summary>The remaining time in seconds. Never below zero.</summary>
    public readonly double TimeLeft => Mathf.Max(0.0, _remaining);

    /// <summary>
    /// How far the countdown has advanced, from 0 (just started) to 1 (finished).
    /// Returns 1 if the timer was never started or has a duration of zero.
    /// </summary>
    public readonly float Progress => _duration != 0.0 ? (float)(1.0 - (_remaining / _duration)) : 1f;

    /// <summary>Stops the countdown immediately, marking it as finished without it being reported by <see cref="Tick(double)"/>.</summary>
    public void Stop() => _remaining = 0.0;

    /// <summary>Starts the countdown using the base duration given at construction.</summary>
    public void Start() => _remaining = _duration = baseDuration;

    /// <summary>Starts the countdown using a custom duration.</summary>
    /// <param name="sec">The duration in seconds.</param>
    public void Start(double sec) => _remaining = _duration = sec;

    /// <summary>Starts the countdown with a random duration between the given bounds.</summary>
    /// <param name="minDuration">The minimum duration in seconds.</param>
    /// <param name="maxDuration">The maximum duration in seconds.</param>
    public void StartRandom(double minDuration, double maxDuration) => Start(GD.RandRange(minDuration, maxDuration));

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

        _remaining -= Mathf.Max(0.0, dt);

        if (_remaining <= 0.0)
        {
            _remaining = 0.0;
            return true;
        }

        return false;
    }
}

